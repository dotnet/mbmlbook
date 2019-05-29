// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Xml.Serialization;
    
    using UnclutteringYourInbox.Features;
    using UnclutteringYourInbox.Models;

    using MBMLViews;

    using Microsoft.ML.Probabilistic.Collections;
    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Math;
    using VariableDictionary = System.Collections.Generic.Dictionary<string, object>;
#if NETFULL
    using Point = System.Windows.Point;
#else
    using Point = MBMLCommon.Point;
#endif

    /// <summary>
    /// The model runner.
    /// </summary>
    public class ModelRunner
    {
        /// <summary>
        /// Loads all input files.
        /// </summary>
        public static void LoadAllInputFiles(string dataPath)
        {
            foreach (var featureSetType in ModelRunner.SingleUserOffline.FeatureSets)
            {
                ModelRunner.SingleUserOffline.Trials[featureSetType].InputsCollection = FileUtils.Load<InputsCollection>(
                    dataPath,
                    featureSetType + "Inputs");
            }

            ModelRunner.OneFeature.Noise.InputsCollection = FileUtils.Load<InputsCollection>(dataPath, "OneFeatureInputs");
            ModelRunner.OneFeature.NoNoise.InputsCollection = ModelRunner.OneFeature.Noise.InputsCollection;
            ModelRunner.CombiningFeatures.Compound.InputsCollection = FileUtils.Load<InputsCollection>(dataPath, "CompoundInputs");
            ModelRunner.CombiningFeatures.Separate.InputsCollection = FileUtils.Load<InputsCollection>(dataPath, "SeparateInputs");
            ModelRunner.SingleUserOnline.Offline.InputsCollection = FileUtils.Load<InputsCollection>(dataPath, "OfflineInputs");

            ModelRunner.CommunityOnline.Seed1Inputs = FileUtils.Load<InputsCollection>(dataPath, "Seed1Inputs");
            ModelRunner.CommunityOnline.Seed2Inputs = FileUtils.Load<InputsCollection>(dataPath, "Seed2Inputs");
            ModelRunner.CommunityOnline.Personalisation1Inputs = FileUtils.Load<InputsCollection>(dataPath, "Personalisation1Inputs");
            ModelRunner.CommunityOnline.Personalisation2Inputs = FileUtils.Load<InputsCollection>(dataPath, "Personalisation2Inputs");
            ModelRunner.CommunityOnline.Online.InputsCollection.Add(ModelRunner.CommunityOnline.Personalisation1Inputs);
            ModelRunner.CommunityOnline.Online.InputsCollection.Add(ModelRunner.CommunityOnline.Personalisation2Inputs);
        }

        public static Dictionary<string, Point[]> StepAndGaussianCdfDemo()
        {
            const double MinMax = 10.0;
            var gaussian = new Gaussian(0.0, 10.0);

            return new Dictionary<string, Point[]>
                       {
                           {
                               "Noiseless model",
                               new[]
                                   {
                                       new Point(-MinMax, 0.0), new Point(0.0, 0.0), new Point(0.0, 1.0),
                                       new Point(MinMax, 1.0)
                                   }
                           },
                           {
                               "Noisy model",
                               new RealRange { Min = -MinMax, Max = MinMax, Steps = 101 }.Values.Select(
                                   x => new Point(x, gaussian.CumulativeDistributionFunction(x))).ToArray()
                           }
                       };
        }

        public static VariableDictionary LogisticDemo()
        {
            var figures = new VariableDictionary();
            const double MinMax = 10.0;
            var gaussian = new Gaussian(0.0, 10.0);

            figures["LogisticAndGaussianCdfDemo"] = new Dictionary<string, Point[]>
                       {
                           {
                               "Logistic function",
                               new RealRange { Min = -MinMax, Max = MinMax, Steps = 101 }.Values.Select(
                                   x => new Point(x, MMath.Logistic(1.8*x/Math.Sqrt(gaussian.GetVariance())))).ToArray()
                           },
                           {
                               "Gaussian CDF",
                               new RealRange { Min = -MinMax, Max = MinMax, Steps = 101 }.Values.Select(
                                   x => new Point(x, gaussian.CumulativeDistributionFunction(x))).ToArray()
                           }
                       };
            figures["StandardLogistic"] = new Dictionary<string, Point[]>
                       {
                           {
                               "Logistic function",
                               new RealRange { Min = -MinMax, Max = MinMax, Steps = 101 }.Values.Select(
                                   x => new Point(x, MMath.Logistic(x))).ToArray()
                           }
                       };

            return figures;
        }

        public static Gamma[] GammasDemo() => new[]
                               {
                                   Gamma.FromShapeAndScale(1.0, 1.0),
                                   Gamma.FromShapeAndScale(1.0, 2.0),
                                   Gamma.FromShapeAndScale(2.0, 1.0),
                                   Gamma.FromShapeAndScale(4.0, 0.5),
                                   Gamma.FromShapeAndScale(8.0, 0.25),
                                   Gamma.FromShapeAndScale(16.0, 0.25)
                               };

        /// <summary>
        /// The one feature model runner.
        /// </summary>
        public static class OneFeature
        {
            /// <summary>
            /// Initializes static members of the <see cref="OneFeature"/> class. 
            /// </summary>
            static OneFeature()
            {
                NoNoise = new Trial { Name = "No Noise" };
                Noise = new Trial { Name = "Noise" };
            }

            /// <summary>
            /// Gets or sets the one feature trial.
            /// </summary>
            public static Trial Noise { get; set; }

            /// <summary>
            /// Gets or sets the one feature trial.
            /// </summary>
            public static Trial NoNoise { get; set; }

            /// <summary>
            /// Gets or sets the priors.
            /// </summary>
            public static Priors Priors { get; set; }

            /// <summary>
            /// Gets the analysis.
            /// </summary>
            public static Dictionary<string, object> Analysis
            {
                get
                {
                    var inputs = Noise.InputsCollection.Inputs[0];
                    var dataSet = inputs.TrainAndValidation;

                    // Use second experiment (with noise)
                    var results = Noise.ExperimentCollection.Experiments[0].Results[0];

                    // Find instances where value is off and on
                    int idxOff = inputs.Validation.Instances.FindIndex(ia => ia.FeatureValues.First().Value < 0.5);
                    int idxOn = inputs.Validation.Instances.FindIndex(ia => ia.FeatureValues.First().Value > 0.5);

                    var predictedProbabilities = new Dictionary<string, object>
                                                 {
                                                     { inputs.FeatureSet.Features[0].Name,  new[] { 0.0, 1.0 } },
                                                     {
                                                         "P(repliedTo=true)",
                                                         new[]
                                                             {
                                                                 new Bernoulli(results.Validation.IsRepliedProbTrue[idxOff]),
                                                                 new Bernoulli(results.Validation.IsRepliedProbTrue[idxOn])
                                                             }
                                                     }
                                                 };

                    var pos = new[]
                              {
                                  dataSet.PositiveInstances.Count(ia => ia.FeatureValues.First().Value < 0.5),
                                  dataSet.PositiveInstances.Count(ia => ia.FeatureValues.First().Value > 0.5)
                              };
                    var neg = new[]
                              {
                                  dataSet.NegativeInstances.Count(ia => ia.FeatureValues.First().Value < 0.5),
                                  dataSet.NegativeInstances.Count(ia => ia.FeatureValues.First().Value > 0.5)
                              };

                    Func<int, int, Bernoulli> fraction = (p, n) => new Bernoulli((double)p / (p + n));

                    var dict = new Dictionary<string, object>
                               {
                                   { inputs.FeatureNames[0], new[] { 0.0, 1.0 } },
                                   { "Replied to", pos },
                                   { "Not replied to", neg },
                               };

                    dict["Fraction replied to"] = new[] { fraction(pos[0], neg[0]), fraction(pos[1], neg[1]) };

                    return new Dictionary<string, object>
                               {
                                   { "Priors", Priors },
                                   { "Posteriors", results.Posteriors },
                                   { "Histograms", dataSet.FeatureHistograms },
                                   { "PredictedProbabilities", predictedProbabilities },
                                   { "Counts", dict }
                               };
                }
            }

            /// <summary>
            /// Runs the one feature experiments.
            /// </summary>
            /// <param name="showFactorGraph">if set to <c>true</c> [show factor graph].</param>
            /// <param name="thresholdAndNoiseVariance">The threshold and noise variance.</param>
            /// <returns>
            /// The <see cref="VariableDictionary" />.
            /// </returns>
            internal static VariableDictionary Run(bool showFactorGraph, double thresholdAndNoiseVariance)
            {
                // Run one feature experiments (with and without noise)
                using (new CodeTimer("Running one feature experiment"))
                {

                    NoNoise.Run<OneFeatureNoNoiseModel>(
                        ExperimentMode.Offline,
                        showFactorGraph,
                        thresholdAndNoiseVariance,
                        "OneFeatureWithoutNoise");

                    Noise.Run<OneFeatureModel>(
                        ExperimentMode.Offline,
                        showFactorGraph,
                        thresholdAndNoiseVariance,
                        "OneFeatureWithNoise",
                        trainMode: InputMode.TrainAndValidation);
                    
                    return new VariableDictionary { { "OneFeature", Noise }, { "OneFeatureAnalysis", Analysis } };
                }
            }
        }

        /// <summary>
        /// The feature testing runner.
        /// </summary>
        public static class CombiningFeatures
        {
            /// <summary>
            /// Initializes static members of the <see cref="CombiningFeatures"/> class. 
            /// </summary>
            static CombiningFeatures()
            {
                Compound = new Trial { Name = "Compound" };
                Separate = new Trial { Name = "Separate" };
            }

            /// <summary>
            /// Gets or sets the separate.
            /// </summary>
            public static Trial Separate { get; set; }

            /// <summary>
            /// Gets or sets the compound.
            /// </summary>
            public static Trial Compound { get; set; }

            /// <summary>
            /// Gets the analysis.
            /// </summary>
            internal static Dictionary<string, object> SeparateAnalysis
            {
                get
                {
                    var inputs = Separate.InputsCollection.Inputs[0];
                    var results = Separate.ExperimentCollection.Experiments[0].Results[0];
                    var dataSet = inputs.TrainAndValidation;

                    // Find instances where value is off and on
                    int idxOffOff = inputs.Validation.Instances.FindIndex(ia => ia.FeatureValues.First().Value < 0.5 && ia.FeatureValues.Last().Value < 0.5);
                    int idxOnOff = inputs.Validation.Instances.FindIndex(ia => ia.FeatureValues.First().Value > 0.5 && ia.FeatureValues.Last().Value < 0.5);
                    int idxOffOn = inputs.Validation.Instances.FindIndex(ia => ia.FeatureValues.First().Value < 0.5 && ia.FeatureValues.Last().Value > 0.5);
                    int idxOnOn = inputs.Validation.Instances.FindIndex(ia => ia.FeatureValues.First().Value > 0.5 && ia.FeatureValues.Last().Value > 0.5);

                    var pos = new[]
                              {
                                  dataSet.PositiveInstances.Count(ia => ia.FeatureValues.First().Value < 0.5 && ia.FeatureValues.Last().Value < 0.5),
                                  dataSet.PositiveInstances.Count(ia => ia.FeatureValues.First().Value > 0.5 && ia.FeatureValues.Last().Value < 0.5),
                                  dataSet.PositiveInstances.Count(ia => ia.FeatureValues.First().Value < 0.5 && ia.FeatureValues.Last().Value > 0.5),
                                  dataSet.PositiveInstances.Count(ia => ia.FeatureValues.First().Value > 0.5 && ia.FeatureValues.Last().Value > 0.5)
                              };
                    var neg = new[]
                              {
                                  dataSet.NegativeInstances.Count(ia => ia.FeatureValues.First().Value < 0.5 && ia.FeatureValues.Last().Value < 0.5),
                                  dataSet.NegativeInstances.Count(ia => ia.FeatureValues.First().Value > 0.5 && ia.FeatureValues.Last().Value < 0.5),
                                  dataSet.NegativeInstances.Count(ia => ia.FeatureValues.First().Value < 0.5 && ia.FeatureValues.Last().Value > 0.5),
                                  dataSet.NegativeInstances.Count(ia => ia.FeatureValues.First().Value > 0.5 && ia.FeatureValues.Last().Value > 0.5)
                              };

                    Func<int, int, Bernoulli> fraction = (p, n) => new Bernoulli((double)p / (p + n));

                    var dict = new Dictionary<string, object>
                               {
                                   { inputs.FeatureNames[0], new[] { 0.0, 1.0, 0.0, 1.0 } },
                                   { inputs.FeatureNames[1], new[] { 0.0, 0.0, 1.0, 1.0 } },
                                   { "Replied to", pos },
                                   { "Not replied to", neg },
                               };

                    dict["Fraction replied to"] = new[] { fraction(pos[0], neg[0]), fraction(pos[1], neg[1]), fraction(pos[2], neg[2]), fraction(pos[3], neg[3]) };
                    dict["P(repliedTo=true)"] = new[]
                                                    {
                                                        new Bernoulli(results.Validation.IsRepliedProbTrue[idxOffOff]),
                                                        new Bernoulli(results.Validation.IsRepliedProbTrue[idxOnOff]),
                                                        new Bernoulli(results.Validation.IsRepliedProbTrue[idxOffOn]),
                                                        new Bernoulli(results.Validation.IsRepliedProbTrue[idxOnOn])
                                                    };

                    return dict;
                }
            }

            /// <summary>
            /// Gets the compound analysis.
            /// </summary>
            internal static Dictionary<string, object> CompoundAnalysis
            {
                get
                {
                    var inputs = Compound.InputsCollection.Inputs[0];
                    var results = Compound.ExperimentCollection.Experiments[0].Results[0];
                    var dataSet = inputs.TrainAndValidation;
                    
                    var f0 = inputs.FeatureSet.Features[0];
                    var f1 = inputs.FeatureSet.Features[1];
                    var f2 = inputs.FeatureSet.Features[2];

                    var b0 = f0.Buckets[0];
                    var b1 = f1.Buckets[0];
                    var b2 = f2.Buckets[0];

                    // Find instances where value is off and on
                    int idxOffOffOff = inputs.Validation.Instances.FindIndex(ia => ia.FeatureValues[b0] < 0.5 && ia.FeatureValues[b1] < 0.5 && ia.FeatureValues[b2] < 0.5);
                    int idxOnOffOff = inputs.Validation.Instances.FindIndex(ia => ia.FeatureValues[b0] > 0.5 && ia.FeatureValues[b1] < 0.5 && ia.FeatureValues[b2] < 0.5);
                    int idxOffOnOff = inputs.Validation.Instances.FindIndex(ia => ia.FeatureValues[b0] < 0.5 && ia.FeatureValues[b1] > 0.5 && ia.FeatureValues[b2] < 0.5);
                    int idxOnOnOn = inputs.Validation.Instances.FindIndex(ia => ia.FeatureValues[b0] > 0.5 && ia.FeatureValues[b1] > 0.5 && ia.FeatureValues[b2] > 0.5);

                    var pos = new[]
                        {
                            dataSet.PositiveInstances.Count(ia => ia.FeatureValues[b0] < 0.5 && ia.FeatureValues[b1] < 0.5 && ia.FeatureValues[b2] < 0.5),
                            dataSet.PositiveInstances.Count(ia => ia.FeatureValues[b0] > 0.5 && ia.FeatureValues[b1] < 0.5 && ia.FeatureValues[b2] < 0.5),
                            dataSet.PositiveInstances.Count(ia => ia.FeatureValues[b0] < 0.5 && ia.FeatureValues[b1] > 0.5 && ia.FeatureValues[b2] < 0.5),
                            dataSet.PositiveInstances.Count(ia => ia.FeatureValues[b0] > 0.5 && ia.FeatureValues[b1] > 0.5 && ia.FeatureValues[b2] > 0.5)
                        };
                    var neg = new[]
                        {
                            dataSet.NegativeInstances.Count(ia => ia.FeatureValues[b0] < 0.5 && ia.FeatureValues[b1] < 0.5 && ia.FeatureValues[b2] < 0.5),
                            dataSet.NegativeInstances.Count(ia => ia.FeatureValues[b0] > 0.5 && ia.FeatureValues[b1] < 0.5 && ia.FeatureValues[b2] < 0.5),
                            dataSet.NegativeInstances.Count(ia => ia.FeatureValues[b0] < 0.5 && ia.FeatureValues[b1] > 0.5 && ia.FeatureValues[b2] < 0.5),
                            dataSet.NegativeInstances.Count(ia => ia.FeatureValues[b0] > 0.5 && ia.FeatureValues[b1] > 0.5 && ia.FeatureValues[b2] > 0.5)
                        };

                    Func<int, int, Bernoulli> fraction = (p, n) => new Bernoulli((double)p / (p + n));

                    var dict = new Dictionary<string, object>
                                   {
                                       { f0.Name, new[] { 0.0, 1.0, 0.0, 1.0 } },
                                       { f1.Name, new[] { 0.0, 0.0, 1.0, 1.0 } },
                                       { f2.Name, new[] { 0.0, 0.0, 0.0, 1.0 } },
                                       { "Replied to", pos },
                                       { "Not replied to", neg },
                                   };

                    dict["Fraction replied to"] = Enumerable.Range(0, 4).Select(i => fraction(pos[i], neg[i])).ToArray();
                    dict["P(repliedTo=true)"] = new[]
                                                    {
                                                        new Bernoulli(results.Validation.IsRepliedProbTrue[idxOffOffOff]),
                                                        new Bernoulli(results.Validation.IsRepliedProbTrue[idxOnOffOff]),
                                                        new Bernoulli(results.Validation.IsRepliedProbTrue[idxOffOnOff]),
                                                        new Bernoulli(results.Validation.IsRepliedProbTrue[idxOnOnOn])
                                                    };

                    return dict;
                }
            }

            /// <summary>
            /// Creates the inputs.
            /// </summary>
            /// <param name="user">The user.</param>
            internal static void CreateInputs(User user)
            {
                var inputs = Inputs.FromUser(user, FeatureSetType.Separate);
                Separate.InputsCollection.Add(inputs);

                var toLineFeature = (BinaryFeature)inputs.FeatureSet.Features[0];
                var managerFeature = (BinaryFeature)inputs.FeatureSet.Features[1];
                var compoundFeature = new And(toLineFeature, managerFeature);

                var toLineBucket = toLineFeature.Buckets[0];
                var managerBucket = managerFeature.Buckets[0];
                var compoundBucket = compoundFeature.Buckets[0];
                
                // Add fake inputs
                var hists = inputs.Train.GetFeatureHistograms(inputs.FeatureSet.Features);
                var toLineFalseFraction = hists[toLineFeature][false.ToString()].Fraction;

                const int NumInstances = 250;

                var train = GetRandomInstances(NumInstances, inputs.FeatureSet, toLineFalseFraction, toLineBucket, managerBucket, 100);
                var validation = GetRandomInstances(NumInstances, inputs.FeatureSet, toLineFalseFraction, toLineBucket, managerBucket, 200);
                
                inputs.Train.Instances = inputs.Train.Instances.Concat(train).ToArray();
                inputs.Validation.Instances = inputs.Validation.Instances.Concat(validation).ToArray();
                inputs.TrainAndValidation.Instances = inputs.Train.Instances.Concat(inputs.Validation.Instances).ToArray();

                // Create Compound feature set
                var compoundFeatureSet = new FeatureSet
                                             {
                                                 Name = "Compound",
                                                 Features = new List<IFeature> { toLineFeature, managerFeature, compoundFeature }
                                             };

                Func<Inputs.Instance, Inputs.Instance> compoundFunc = instance =>
                    {
                        var featureValues = new Dictionary<FeatureBucket, double>(instance.FeatureValues);
                        featureValues[compoundBucket] = instance.FeatureValues[toLineBucket] * instance.FeatureValues[managerBucket];
                        return new Inputs.Instance { FeatureValues = featureValues, Label = instance.Label, FeatureSet = compoundFeatureSet };
                    };

                var trainInstances = inputs.Train.Instances.Select(compoundFunc).ToArray();
                var validationInstances = inputs.Validation.Instances.Select(compoundFunc).ToArray();
                var trainAndValidationInstances = inputs.TrainAndValidation.Instances.Select(compoundFunc).ToArray();

                var compoundInputs = new Inputs
                    {
                        FeatureSet = compoundFeatureSet,
                        UserName = user.Name.ToString(),
                        Train = new Inputs.DataSet { Instances = trainInstances, Name = "Train", FeatureSet = compoundFeatureSet },
                        Validation = new Inputs.DataSet { Instances = validationInstances, Name = "Validation", FeatureSet = compoundFeatureSet },
                        TrainAndValidation = new Inputs.DataSet { Instances = trainAndValidationInstances, Name = (InputMode.Training | InputMode.Validation).ToString(), FeatureSet = compoundFeatureSet }
                    };
                
                Compound.InputsCollection.Add(compoundInputs);
            }

            /// <summary>
            /// Runs the combined feature experiments.
            /// </summary>
            /// <param name="showFactorGraph">if set to <c>true</c> [show factor graph].</param>
            /// <param name="thresholdAndNoiseVariance">The threshold and noise variance.</param>
            /// <returns>
            /// The <see cref="VariableDictionary" />.
            /// </returns>
            internal static VariableDictionary Run(bool showFactorGraph, double thresholdAndNoiseVariance)
            {
                using (new CodeTimer("Running feature testing experiments"))
                {
                    Separate.Run<ReplyToModel>(ExperimentMode.Offline, showFactorGraph, thresholdAndNoiseVariance, "ReplyTo", trainMode: InputMode.TrainAndValidation);

                    Compound.Run<ReplyToModel>(ExperimentMode.Offline, showFactorGraph, thresholdAndNoiseVariance, "ReplyTo", trainMode: InputMode.TrainAndValidation);
                    
                    return new VariableDictionary
                               {
                                   { "SeparateAnalysis", SeparateAnalysis },
                                   { "CompoundAnalysis", CompoundAnalysis },
                                   { "Separate", Separate },
                                   { "Compound", Compound }
                               };
                }
            }

            /// <summary>
            /// Gets the random instances.
            /// </summary>
            /// <param name="numInstances">The number instances.</param>
            /// <param name="featureSet">The feature set.</param>
            /// <param name="toLineFalseFraction">To line false fraction.</param>
            /// <param name="toLineBucket">To line bucket.</param>
            /// <param name="managerBucket">The manager bucket.</param>
            /// <param name="seed">The seed.</param>
            /// <returns>
            /// The <see cref="IEnumerable{Instance}" />
            /// </returns>
            private static IEnumerable<Inputs.Instance> GetRandomInstances(
                int numInstances,
                FeatureSet featureSet,
                double toLineFalseFraction,
                FeatureBucket toLineBucket,
                FeatureBucket managerBucket,
                int seed)
            {
                var random = new Random(seed);

                var instances = new List<Inputs.Instance>();

                for (int i = 0; i < numInstances; i++)
                {
                    Dictionary<FeatureBucket, double> featureValues;
                    bool label;

                    if (i < numInstances / 2)
                    {
                        featureValues = new Dictionary<FeatureBucket, double> { { toLineBucket, 0.0 }, { managerBucket, 1.0 } };
                        label = i < numInstances * toLineFalseFraction / 2;
                    }
                    else
                    {
                        featureValues = new Dictionary<FeatureBucket, double> { { toLineBucket, 1.0 }, { managerBucket, 1.0 } };

                        // 80% chance (use 0.9 because half of the instances are used already
                        label = i < numInstances * 0.9;
                    }

                    instances.Add(new Inputs.Instance { FeatureSet = featureSet, FeatureValues = featureValues, Label = label });
                }

                var instancesShuffled = instances.OrderBy(ia => random.Next());

                return instancesShuffled;
            }
        }

        /// <summary>
        /// The batch versus online model runner.
        /// </summary>
        public static class SingleUserOnline
        {
            /// <summary>
            /// Initializes static members of the <see cref="SingleUserOnline"/> class.
            /// </summary>
            static SingleUserOnline()
            {
                Offline = new Trial { Name = "Offline" };
                Online = new Dictionary<int, Trial>();
            }

            /// <summary>
            /// Gets or sets the offline trial.
            /// </summary>
            public static Trial Offline { get; set; }

            /// <summary>
            /// Gets or sets the online trials.
            /// </summary>
            public static Dictionary<int, Trial> Online { get; set; }

            /// <summary>
            /// Runs this instance.
            /// </summary>
            /// <param name="showFactorGraph">if set to <c>true</c> [show factor graph].</param>
            /// <param name="thresholdAndNoiseVariance">The threshold and noise variance.</param>
            /// <returns>
            /// The <see cref="VariableDictionary" />
            /// </returns>
            public static VariableDictionary Run(bool showFactorGraph, double thresholdAndNoiseVariance)
            {
                Offline.InputsCollection = SingleUserOffline.Trials[FeatureSetType.WithRecipient].InputsCollection;

                // Use the size of the smallest dataset
                int limit = Offline.InputsCollection.Inputs.Min(ia => ia.Train.Count);

                Offline.Run(ExperimentMode.Incremental, showFactorGraph, thresholdAndNoiseVariance, 1, limit);

                var batchSizes = new[] { 1, 5, 10, 50 };
                int i = 0;
                foreach (int batchSize in batchSizes)
                {
                    var trial = new Trial
                                    {
                                        Name = string.Format("Online ({0})", batchSize),
                                        InputsCollection = Offline.InputsCollection,
                                    };
                    Online.Add(batchSize, trial);
                    trial.Run(ExperimentMode.Online, showFactorGraph, thresholdAndNoiseVariance, batchSize, limit);
                }

                var auc = Online.ToDictionary(ia => ia.Key.ToString("D"), ia => ia.Value.ExperimentCollection.AreaUnderCurveOnlineAverage);
                auc["Offline"] = Offline.ExperimentCollection.AreaUnderCurveOnlineAverage;

                var ap = Online.ToDictionary(ia => ia.Key.ToString("D"), ia => ia.Value.ExperimentCollection.AveragePrecisionOnlineAverage);
                ap["Offline"] = Offline.ExperimentCollection.AveragePrecisionOnlineAverage;

                var variables = new VariableDictionary
                                    {
                                        {
                                            "SingleUserOnline",
                                            new Dictionary<string, object>
                                                {
                                                    { "AveragePrecision", ap },
                                                    { "AreaUnderCurve", auc }
                                                }
                                        },
                                        { "Offline", Offline },
                                        { "Online", Online },
                                    };

                return variables;
            }
        }

        /// <summary>
        /// The individual model runner.
        /// </summary>
        public static class SingleUserOffline
        {
            /// <summary>
            /// The feature sets.
            /// </summary>
            internal static readonly IList<FeatureSetType> FeatureSets = new[]
                {
                    FeatureSetType.Initial,
                    FeatureSetType.WithSubjectPrefix, 
                    FeatureSetType.WithRecipient
                };
                
            /// <summary>
            /// Initializes static members of the <see cref="SingleUserOffline"/> class. 
            /// </summary>
            static SingleUserOffline()
            {
                Trials = FeatureSets.ToDictionary(ia => ia, ia => new Trial { Name = "Individual" + ia });
            }

            /// <summary>
            /// Gets or sets the trials.
            /// </summary>
            public static Dictionary<FeatureSetType, Trial> Trials { get; set; }
            
            /// <summary>
            /// Gets or sets the threshold prior testing.
            /// </summary>
            public static ExperimentCollection ThresholdPriorTesting { get; set; }

            /// <summary>
            /// Runs the threshold prior testing.
            /// </summary>
            /// <param name="showFactorGraph">if set to <c>true</c> [show factor graph].</param>
            /// <param name="batchSize">Size of the batch.</param>
            internal static void RunThresholdPriorTesting(bool showFactorGraph, int batchSize)
            {
                ThresholdPriorTesting = new ExperimentCollection();
                
                foreach (var inputs in Trials[FeatureSetType.Initial].InputsCollection.Inputs)
                {
                    var experiment = new Experiment
                    {
                        UserName = inputs.UserName,
                        BatchSize = batchSize,
                        Mode = ExperimentMode.Offline
                    };

                    ThresholdPriorTesting.Add(experiment);

                    var trainModel = new SparseReplyToModel
                                         {
                                             Name = "ReplyTo",
                                             ShowFactorGraph = showFactorGraph,
                                             Mode = InputMode.Training
                                         };
                    var testModel = new SparseReplyToModel { Name = "ReplyTo", ShowFactorGraph = showFactorGraph, Mode = InputMode.Testing };

                    // Experiments with different threshold priors
                    foreach (var thresholdVariance in Enumerable.Range(1, 10))
                    {
                        experiment.Run(trainModel, testModel, inputs, Priors.Generate(inputs.FeatureSet.FeatureBuckets, 1.0, thresholdVariance), thresholdVariance);
                    }
                }
            }

            /// <summary>
            /// Creates the inputs.
            /// </summary>
            /// <param name="user">The user.</param>
            internal static void CreateInputs(User user)
            {
                FeatureSets.ForEach(ia => Trials[ia].InputsCollection.Add(Inputs.FromUser(user, ia)));
            }

            /// <summary>
            /// Runs the individual experiments.
            /// </summary>
            /// <param name="showFactorGraph">if set to <c>true</c> [show factor graph].</param>
            /// <param name="thresholdAndNoiseVariance">The threshold and noise variance.</param>
            /// <returns>
            /// The <see cref="VariableDictionary" />.
            /// </returns>
            internal static VariableDictionary Run(bool showFactorGraph, double thresholdAndNoiseVariance)
            {
                using (new CodeTimer("Running individual experiments"))
                {
                    double increment = 90.0 / Trials.Count;

                    foreach (var trial in Trials)
                    {
                        trial.Value.Run<SparseReplyToModel>(
                            ExperimentMode.Offline,
                            showFactorGraph,
                            thresholdAndNoiseVariance,
                            "ReplyTo",
                            testMode: InputMode.Validation | InputMode.Testing);
                    }
                    
                    var func =
                        new Func<Dictionary<FeatureSetType, Trial>, Func<Trial, object>, Dictionary<string, object>>(
                            (d, f) => d.ToDictionary(ia => ia.Key.ToString(), ia => f(ia.Value)));

                    var community = Trials[FeatureSetType.WithRecipient].ExperimentCollection.Experiments.Take(CommunityOnline.SeedCount).ToList();
                    
                    var dict = new VariableDictionary
                        {
                            { "Trials", Trials },
                            { "IndividualResults", func(Trials, ia => ia.ExperimentCollection.OverallMetricsWithAverage) },
                            { "IndividualResultsWithTest", func(Trials, ia => ia.ExperimentCollection.OverallMetricsWithTestAndAverage) },
                            { "FeatureSets", func(Trials, ia => ia.InputsCollection.Inputs[0].FeatureSet.Features.Select(f => f.Name).ToArray()) },
                            { "DataSetSizes", Trials[FeatureSetType.Initial].InputsCollection.DataSetSizes },
                            { "TopSenders", Trials[FeatureSetType.Initial].InputsCollection.Inputs[0].TopSenders },
                            { "FeatureSetDescriptions", func(Trials, ia => ia.InputsCollection.Inputs[0].FeatureSet.FeatureDescriptions) },
                            { "PrecisionRecallCurves", func(Trials, ia => ia.ExperimentCollection.PrecisionRecallCurves) },
                            { "PrecisionRecallTestCurves", func(Trials, ia => ia.ExperimentCollection.PrecisionRecallTestCurves) },
                            { "RocCurves", func(Trials, ia => ia.ExperimentCollection.RocCurves) },
                            { "PosteriorMeans", func(Trials, ia => ia.ExperimentCollection.Experiments[0].Results[0].Posteriors.Means) },
                            { "PosteriorMeansWithSDs", func(Trials, ia => ia.ExperimentCollection.Experiments[0].Results[0].Posteriors.MeansAndStandardDeviations) },
                            { "CalibrationCurves", func(Trials, ia => ia.ExperimentCollection.CalibrationCurves) },
                            { "CalibrationCurvesWithAverage", func(Trials, ia => ia.ExperimentCollection.CalibrationCurvesWithAverage) },
                            { "AverageCalibrationCurve", func(Trials, ia => ia.ExperimentCollection.AverageCalibrationCurve) },
                            { "FeatureHistograms", Trials[FeatureSetType.WithSubjectPrefix].InputsCollection.FeatureHistograms },
                            { "SharedWeights", community.ToDictionary(ia => ia.UserName, ia => ia.Results[0].Posteriors.SharedMeans) },
                            { "SharedWeightsWithSDs", community.ToDictionary(ia => ia.UserName, ia => ia.Results[0].Posteriors.SharedMeansAndStandardDeviations) }
                        };
                    
                    return dict;
                }
            }
        }

        /// <summary>
        /// The timing.
        /// </summary>
        public static class FeaturesWithManyStates
        {
            /// <summary>
            /// Initializes static members of the <see cref="FeaturesWithManyStates"/> class.
            /// </summary>
            static FeaturesWithManyStates()
            {
                NonSparse = new Trial { Name = "NonSparse" };
                Sparse = new Trial { Name = "Sparse" };
            }

            /// <summary>
            /// Gets or sets the non sparse.
            /// </summary>
            public static Trial NonSparse { get; set; }

            /// <summary>
            /// Gets or sets the sparse.
            /// </summary>
            public static Trial Sparse { get; set; }

            /// <summary>
            /// Runs this instance.
            /// </summary>
            /// <param name="variables">The variables.</param>
            /// <param name="thresholdAndNoiseVariance">The threshold and noise variance.</param>
            public static Dictionary<string, TimeSpan> Run(double thresholdAndNoiseVariance)
            {
                NonSparse.InputsCollection.Add(SingleUserOffline.Trials[FeatureSetType.Initial].InputsCollection.Inputs[0]);
                Sparse.InputsCollection.Add(SingleUserOffline.Trials[FeatureSetType.Initial].InputsCollection.Inputs[0]);

                NonSparse.Run<ReplyToModel>(ExperimentMode.Offline, false, thresholdAndNoiseVariance, "NonSparse");

                Sparse.Run<SparseReplyToModel>(ExperimentMode.Offline, false, thresholdAndNoiseVariance, "Sparse");

                return new[] { NonSparse, Sparse }.ToDictionary(ia => ia.Name, ia => ia.ExperimentCollection.Experiments[0].TrainTimings[0]);
            }
        }

        /// <summary>
        /// The community versus online.
        /// </summary>
        public static class CommunityOnline
        {
            /// <summary>
            /// Initializes static members of the <see cref="CommunityOnline"/> class. 
            /// </summary>
            static CommunityOnline()
            {
                SeedCount = 5;
                Online = new Trial { Name = "Online" };
                Seed1Inputs = new InputsCollection();
                Seed2Inputs = new InputsCollection();
                Personalisation1Inputs = new InputsCollection();
                Personalisation2Inputs = new InputsCollection();
                CommunityExperiments = new ExperimentCollection { Name = "Community-Online" };
                CommunityFeatureSet = FeatureSet.GetCommunitySet(FeatureSetType.WithRecipient2);
            }

            /// <summary>
            /// Gets or sets the seed count.
            /// </summary>
            public static int SeedCount { get; set; }

            /// <summary>
            /// Gets or sets the seed 1 inputs.
            /// </summary>
            public static InputsCollection Seed1Inputs { get; set; }

            /// <summary>
            /// Gets or sets the seed 2 inputs.
            /// </summary>
            public static InputsCollection Seed2Inputs { get; set; }

            /// <summary>
            /// Gets or sets the personalisation 1 inputs.
            /// </summary>
            public static InputsCollection Personalisation1Inputs { get; set; }

            /// <summary>
            /// Gets or sets the personalisation 2 inputs.
            /// </summary>
            public static InputsCollection Personalisation2Inputs { get; set; }

            /// <summary>
            /// Gets or sets the community experiments.
            /// </summary>
            public static ExperimentCollection CommunityExperiments { get; set; }

            /// <summary>
            /// Gets or sets the community trial.
            /// </summary>
            public static ExperimentCollection CommunityFixedPrecision { get; set; }

            /// <summary>
            /// Gets or sets the community trial.
            /// </summary>
            public static ExperimentCollection CommunityNonHierarchical { get; set; }

            /// <summary>
            /// Gets or sets the online trial.
            /// </summary>
            public static Trial Online { get; set; }
            
            /// <summary>
            /// Gets the metrics.
            /// </summary>
            public static Dictionary<string, object> CommunityMetrics
            {
                get
                {
                    if (CommunityExperiments.Experiments == null || CommunityExperiments.Experiments.Count == 0)
                    {
                        return null;
                    }

                    IList<ExperimentCollection> collections = new[] { Online.ExperimentCollection, CommunityExperiments };

                    if (CommunityFixedPrecision != null)
                    {
                        collections.Add(CommunityFixedPrecision);
                    }

                    if (CommunityNonHierarchical != null)
                    {
                        collections.Add(CommunityNonHierarchical);
                    }

                    Func<ExperimentCollection, Point[]> ap = ia => ia.AveragePrecisionOnlineAverage;
                    Func<ExperimentCollection, Point[]> auc = ia => ia.AreaUnderCurveOnlineAverage;

                    Func<Func<Experiment, Point[]>, Dictionary<string, Point[]>> zippers = f =>
                        {
                            var dict = new Dictionary<string, Point[]>();
                            foreach (var experiment in CommunityExperiments.Experiments)
                            {
                                var oe = Online.ExperimentCollection.Experiments.Find(e => e.UserName == experiment.UserName);
                                if (oe == null)
                                {
                                    continue;
                                }

                                dict[experiment.UserName + " (community)"] = f(experiment);
                                dict[experiment.UserName + " (online)"] = f(oe);
                            }

                            return dict;
                        };

                    return new Dictionary<string, object>
                        {
                            { "AreaUnderCurve", zippers(ia => ia.Metrics.Select((m, i) => new Point(i * ia.BatchSize, m.Validation.AreaUnderCurve)).ToArray()) },
                            { "AveragePrecision", zippers(ia => ia.Metrics.Select((m, i) => new Point(i * ia.BatchSize, m.Validation.AveragePrecision)).ToArray()) },
                            { "AverageAreaUnderCurve", collections.ToDictionary(ia => ia.Name, auc) },
                            { "AverageAveragePrecision", collections.ToDictionary(ia => ia.Name, ap) }
                        };
                }
            }

            /// <summary>
            /// Gets or sets the community feature set.
            /// </summary>
            public static FeatureSet CommunityFeatureSet { get; set; }

            /// <summary>
            /// Creates the inputs.
            /// </summary>
            /// <param name="users">The users.</param>
            public static void CreateInputs(IList<User> users)
            {
                foreach (var user in users.Take(SeedCount))
                {
                    var personalFeatureSet = FeatureSet.GetPersonalSet(
                        CommunityFeatureSet,
                        user,
                        FeatureSetType.WithRecipient);
                    var personalInputs = Inputs.FromUser(user, personalFeatureSet);

                    Seed1Inputs.Add(Inputs.FromUser(user, CommunityFeatureSet));
                    Personalisation1Inputs.Add(personalInputs);
                    Online.InputsCollection.Add(personalInputs);
                }

                foreach (var user in users.Skip(SeedCount))
                {
                    var personalFeatureSet = FeatureSet.GetPersonalSet(
                        CommunityFeatureSet,
                        user,
                        FeatureSetType.WithRecipient);
                    var personalInputs = Inputs.FromUser(user, personalFeatureSet);

                    Seed2Inputs.Add(Inputs.FromUser(user, CommunityFeatureSet));
                    Personalisation2Inputs.Add(personalInputs);
                    Online.InputsCollection.Add(personalInputs);
                }
            }

            /// <summary>
            /// Runs the community and online experiments.
            /// </summary>
            /// <param name="showFactorGraph">if set to <c>true</c> [show factor graph].</param>
            /// <param name="thresholdAndNoiseVariance">The threshold and noise variance.</param>
            /// <returns>
            /// The <see cref="VariableDictionary" />.
            /// </returns>
            internal static VariableDictionary Run(bool showFactorGraph, double thresholdAndNoiseVariance)
            {
                using (new CodeTimer("Running community experiment"))
                {
                    var trainModel = new CommunityModel
                                         {
                                             Name = "Community",
                                             ShowFactorGraph = showFactorGraph,
                                             Mode = InputMode.CommunityTraining
                                         };

                    var updateModel = new CommunityModel { Name = "Personalisation", Mode = InputMode.Training };
                    var testModel = new CommunityModel { Mode = InputMode.Testing };
                    var updateModelNonHierarchical = new SparseReplyToModel2 { Name = "Personalisation", Mode = InputMode.Training };
                    var testModelNonHierarchical = new SparseReplyToModel2 { Mode = InputMode.Testing };

                    const int BatchSize = 5;
                    const double PrecisionShape = 4.0;
                    const double PrecisionScale = 0.5;
                    const double PointMass = 1.0;

                    CommunityPriors priors;
                    CommunityPosteriors posteriors;
                    int limit = Online.InputsCollection.Inputs.Skip(SeedCount).Min(ia => ia.Train.Count);

                    RunCommunityTrials(
                        thresholdAndNoiseVariance,
                        Seed1Inputs.Inputs,
                        Personalisation1Inputs.Inputs,
                        trainModel,
                        updateModel,
                        testModel,
                        updateModelNonHierarchical,
                        testModelNonHierarchical,
                        BatchSize,
                        PrecisionShape,
                        PrecisionScale,
                        PointMass,
                        limit,
                        out priors,
                        out posteriors);

                    RunCommunityTrials(
                        thresholdAndNoiseVariance,
                        Seed2Inputs.Inputs,
                        Personalisation2Inputs.Inputs,
                        trainModel,
                        updateModel,
                        testModel,
                        updateModelNonHierarchical,
                        testModelNonHierarchical,
                        BatchSize,
                        PrecisionShape,
                        PrecisionScale,
                        PointMass,
                        limit,
                        out priors,
                        out posteriors);

                    Online.Run(ExperimentMode.Online, showFactorGraph, thresholdAndNoiseVariance, BatchSize, limit);

                    var dict = new VariableDictionary
                                   {
                                       { "CommunityPriors", priors },
                                       { "CommunityPosteriors", posteriors },
                                       { "CommunityMetrics", CommunityMetrics },
                                       { "Community", CommunityExperiments },
                                       { "Online", Online },
                                       { "Seed1Inputs", Seed1Inputs },
                                       { "Seed2Inputs", Seed2Inputs },
                                       { "Personalisation1Inputs", Personalisation1Inputs },
                                       { "Personalisation2Inputs", Personalisation2Inputs }
                                   };

                    return dict;
                }
            }

            /// <summary>
            /// Runs the community trials.
            /// </summary>
            /// <param name="thresholdAndNoiseVariance">The threshold and noise variance.</param>
            /// <param name="seedInputs">The seed inputs.</param>
            /// <param name="personalisationInputs">The personalisation inputs.</param>
            /// <param name="trainModel">The train model.</param>
            /// <param name="updateModel">The update model.</param>
            /// <param name="testModel">The test model.</param>
            /// <param name="updateModelNonHierarchical">The update model non hierarchical.</param>
            /// <param name="testModelNonHierarchical">The test model non hierarchical.</param>
            /// <param name="batchSize">Size of the batch.</param>
            /// <param name="precisionShape">The precision shape.</param>
            /// <param name="precisionScale">The precision scale.</param>
            /// <param name="pointMass">The point mass.</param>
            /// <param name="limit">The limit.</param>
            /// <param name="priors">The priors.</param>
            /// <param name="posteriors">The posteriors.</param>
            private static void RunCommunityTrials(
                double thresholdAndNoiseVariance,
                IList<Inputs> seedInputs,
                IList<Inputs> personalisationInputs,
                CommunityModelBase trainModel,
                CommunityModelBase updateModel,
                CommunityModelBase testModel,
                ReplyToModelBase updateModelNonHierarchical,
                ReplyToModelBase testModelNonHierarchical,
                int batchSize,
                double precisionShape,
                double precisionScale,
                double pointMass,
                int limit,
                out CommunityPriors priors,
                out CommunityPosteriors posteriors)
            {
                var experiment = new Experiment { Mode = ExperimentMode.Community, UserName = "Community", BatchSize = batchSize };
                var experimentFixedPrecision = new Experiment
                                                   {
                                                       Mode = ExperimentMode.Community,
                                                       UserName = "CommunityFixedPrecision",
                                                       BatchSize = batchSize
                                                   };

                var featureBuckets = CommunityFeatureSet.FeatureBuckets.Where(ia => ia.Feature.IsShared).ToArray();

                priors = CommunityPriors.Generate(
                    featureBuckets,
                    precisionShape,
                    precisionScale,
                    thresholdAndNoiseVariance,
                    seedInputs.Select(ia => ia.UserName));

                var priorsFixedPrecision = CommunityPriors.Generate(
                    featureBuckets,
                    pointMass,
                    thresholdAndNoiseVariance,
                    seedInputs.Select(ia => ia.UserName));

                posteriors = experiment.RunCommunityTraining(trainModel, seedInputs, CommunityFeatureSet, priors, thresholdAndNoiseVariance);

                var posteriorsFixedPrecision = experimentFixedPrecision.RunCommunityTraining(
                    trainModel,
                    seedInputs,
                    CommunityFeatureSet,
                    priorsFixedPrecision,
                    thresholdAndNoiseVariance);

                double pct = 0.0;
                foreach (var inputs in personalisationInputs)
                {
                    // Hierarchical version
                    experiment = new Experiment { Mode = ExperimentMode.Online, UserName = inputs.UserName, BatchSize = batchSize };
                    CommunityExperiments.Add(experiment);

                    var communityPriors = CommunityPriors.FromPosteriors(
                        posteriors,
                        inputs.FeatureSet,
                        precisionShape,
                        precisionScale,
                        thresholdAndNoiseVariance,
                        new[] { inputs.UserName });

                    experiment.RunPersonalisation(
                        updateModel,
                        testModel,
                        inputs,
                        communityPriors,
                        precisionShape,
                        precisionScale,
                        thresholdAndNoiseVariance,
                        limit, 
                        InputMode.Validation);

                    // Fixed precision version
                    if (CommunityFixedPrecision != null)
                    {
                        experiment = new Experiment { Mode = ExperimentMode.Online, UserName = inputs.UserName, BatchSize = batchSize };
                        CommunityFixedPrecision.Add(experiment);

                        var communityPriorsFixedPrecision = CommunityPriors.FromPosteriors(
                            posteriorsFixedPrecision,
                            inputs.FeatureSet,
                            pointMass,
                            thresholdAndNoiseVariance,
                            new[] { inputs.UserName });

                        experiment.RunPersonalisation(
                            updateModel,
                            testModel,
                            inputs,
                            communityPriorsFixedPrecision,
                            pointMass,
                            thresholdAndNoiseVariance,
                            limit,
                            InputMode.Validation);
                    }

                    // Non-hierarchical version
                    if (CommunityNonHierarchical != null)
                    {
                        var communityPriorsNonHierarchical = Priors.FromCommunityPosteriors(
                            posteriors,
                            inputs.FeatureSet,
                            thresholdAndNoiseVariance);

                        experiment = new Experiment { Mode = ExperimentMode.Online, UserName = inputs.UserName, BatchSize = batchSize };
                        CommunityNonHierarchical.Add(experiment);

                        experiment.Run(
                            updateModelNonHierarchical,
                            testModelNonHierarchical,
                            inputs,
                            communityPriorsNonHierarchical,
                            thresholdAndNoiseVariance,
                            limit,
                            testMode: InputMode.Validation);
                    }

                    pct += 60.0 / (personalisationInputs.Count - SeedCount);
                }
            }
        }

        /// <summary>
        /// The trial (inputs and experiments).
        /// </summary>
        public class Trial
        {
            /// <summary>
            /// The name.
            /// </summary>
            private string name;

            /// <summary>
            /// Initializes a new instance of the <see cref="Trial"/> class.
            /// </summary>
            public Trial()
            {
                this.ExperimentCollection = new ExperimentCollection();
                this.InputsCollection = new InputsCollection();
            }

            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            public string Name
            {
                get
                {
                    return this.name;
                }

                set
                {
                    this.name = value;
                    if (this.ExperimentCollection != null)
                    {
                        this.ExperimentCollection.Name = value;
                    }
                }
            }

            /// <summary>
            /// Gets or sets the inputs collection.
            /// </summary>
            public InputsCollection InputsCollection { get; set; }

            /// <summary>
            /// Gets or sets the experiment collection.
            /// </summary>
            public ExperimentCollection ExperimentCollection { get; set; }

            /// <summary>
            /// Gets or sets the validation messages.
            /// </summary>
            [Browsable(false)]
            [XmlIgnore]
            public IList<Message> ValidationMessages { get; set; }

            /// <summary>
            /// Gets or sets the test messages.
            /// </summary>
            [Browsable(false)]
            [XmlIgnore]
            public IList<Message> TestMessages { get; set; }

            /// <summary>
            /// Gets or sets the train messages.
            /// </summary>
            [Browsable(false)]
            [XmlIgnore]
            public IList<Message> TrainMessages { get; set; }

            /// <summary>
            /// Updates the probability of reply. Note this currently only works for batch mode trials.
            /// </summary>
            /// <param name="inputMode">The input mode.</param>
            /// <param name="featureSetType">Type of the feature set.</param>
            public void UpdateProbabilityOfReply(InputMode inputMode, FeatureSetType featureSetType)
            {
                IList<Message> messages;
                double[] isRepliedProbTrue;
                IList<Inputs.Instance> instances;
                Dictionary<FeatureBucket, Gaussian> weights = null;

                switch (inputMode)
                {
                    case InputMode.Training:
                        messages = this.TrainMessages;
                        isRepliedProbTrue = this.ExperimentCollection.Experiments.Last().Results.Last().Train.IsRepliedProbTrue;
                        instances = this.InputsCollection.Inputs.Last().Train.Instances;
                        break;
                    case InputMode.Validation:
                        messages = this.ValidationMessages;
                        isRepliedProbTrue = this.ExperimentCollection.Experiments.Last().Results.Last().Validation.IsRepliedProbTrue;
                        instances = this.InputsCollection.Inputs.Last().Validation.Instances;
                        weights = this.ExperimentCollection.Experiments.Last().Results.Last().Posteriors.Weights;
                        break;
                    case InputMode.Testing:
                        messages = this.TestMessages;
                        isRepliedProbTrue = this.ExperimentCollection.Experiments.Last().Results.Last().Test.IsRepliedProbTrue;
                        instances = this.InputsCollection.Inputs.Last().Test.Instances;
                        weights = this.ExperimentCollection.Experiments.Last().Results.Last().Posteriors.Weights;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("inputMode", @"Unsupported input mode");
                }

                for (int i = 0; i < messages.Count; i++)
                {
                    var message = messages[i];
                    message.ProbabilityOfReplyDictionary[featureSetType] = isRepliedProbTrue[i];
                    message.FeatureValueAndWeightDictionary[featureSetType] = instances[i].FeatureValues.ToDictionary(
                        ia => ia.Key,
                        ia => new Pair<double, Gaussian>(ia.Value, weights == null ? Gaussian.Uniform() : weights[ia.Key]));
                }
            }

            /// <summary>
            /// Runs the specified mode.
            /// </summary>
            /// <param name="mode">The mode.</param>
            /// <param name="showFactorGraph">if set to <c>true</c> [show factor graph].</param>
            /// <param name="thresholdAndNoiseVariance">The threshold and noise variance.</param>
            /// <param name="batchSize">Size of the batch.</param>
            /// <param name="limit">The limit.</param>
            public void Run(
                ExperimentMode mode,
                bool showFactorGraph,
                double thresholdAndNoiseVariance,
                int batchSize = -1,
                int limit = -1)
            {
                this.Run<SparseReplyToModel>(
                    mode,
                    showFactorGraph,
                    thresholdAndNoiseVariance,
                    "ReplyTo",
                    batchSize,
                    limit: limit);
            }

            /// <summary>
            /// Runs the specified mode.
            /// </summary>
            /// <typeparam name="T">The type of the model.</typeparam>
            /// <param name="mode">The mode.</param>
            /// <param name="showFactorGraph">if set to <c>true</c> [show factor graph].</param>
            /// <param name="thresholdAndNoiseVariance">The threshold and noise variance.</param>
            /// <param name="modelName">Name of the model.</param>
            /// <param name="batchSize">Size of the batch.</param>
            /// <param name="priorsFunc">The priors function.</param>
            /// <param name="limit">The limit.</param>
            /// <param name="trainMode">The train mode.</param>
            /// <param name="testMode">The test mode.</param>
            public void Run<T>(
                ExperimentMode mode,
                bool showFactorGraph,
                double thresholdAndNoiseVariance,
                string modelName,
                int batchSize = -1,
                Func<FeatureSet, Priors> priorsFunc = null,
                int limit = -1,
                InputMode trainMode = InputMode.Training,
                InputMode testMode = InputMode.Validation) where T : ReplyToModelBase, new()
            {
                // Clear the experiments first in case this is being rerun
                this.ExperimentCollection.Clear();

                for (int i = 0; i < this.InputsCollection.Inputs.Count; i++)
                {
                    Console.WriteLine(@"{0} iteration {1}/{2}", this.Name, i + 1, this.InputsCollection.Inputs.Count);
                    var inputs = this.InputsCollection.Inputs[i];

                    var experiment = new Experiment { UserName = inputs.UserName, BatchSize = batchSize, Mode = mode };

                    var trainModel = new T { Name = modelName, ShowFactorGraph = showFactorGraph, Mode = InputMode.Training };
                    var testModel = new T { Name = modelName, ShowFactorGraph = showFactorGraph, Mode = InputMode.Testing };

                    var priors = priorsFunc == null
                                     ? Priors.Generate(inputs.FeatureSet.FeatureBuckets, 1.0, thresholdAndNoiseVariance)
                                     : priorsFunc(inputs.FeatureSet);
                
                    this.ExperimentCollection.Add(experiment);
                    experiment.Run(trainModel, testModel, inputs, priors, thresholdAndNoiseVariance, limit, trainMode, testMode);
                }
            }
        }
    }
}