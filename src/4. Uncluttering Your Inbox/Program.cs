// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox
{
    using System;
    using System.Collections.Generic;
    
    using UnclutteringYourInbox.DataCleaning;
    using UnclutteringYourInbox.Features;
    using Microsoft.Research.Glo.Views;

    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Math;
    using Microsoft.Research.Glo.Object;
    using MBMLCommon;
    using static UnclutteringYourInbox.ModelRunner;
#if NETFULL
    using MBMLViews.Views;
#endif

    /// <summary>
    /// The program.
    /// </summary>
    public static class Program
    {
#region Constants
        /// <summary>
        /// The default months.
        /// </summary>
        internal const int DefaultMonths = 6;

        /// <summary>
        /// The max months.
        /// </summary>
        internal const int MaxMonths = 48;

        /// <summary>
        /// The (desired) train set size.
        /// </summary>
        internal const int TrainSetSize = 1500;

        /// <summary>
        /// The (desired) validation set size.
        /// </summary>
        internal const int ValidationSetSize = 1500;

        /// <summary>
        /// The (desired) test set size.
        /// </summary>
        internal const int TestSetSize = 500;

        /// <summary>
        /// The threshold and noise variance.
        /// </summary>
        internal const double ThresholdAndNoiseVariance = 10.0;

        /// <summary>
        /// The data path.
        /// </summary>
        private const string DataPath = @"Data";

#endregion

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        public static void Main(string[] args)
        {
            Outputter outputter = Outputter.GetOutputter(Contents.ChapterName);
            InitializeUI();

            try
            {
                RunExperiments(outputter);
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nAn unhandled exception was thrown:\n{e}");
            }
            finally
            {
                if (args.Length == 1)
                {
                    Console.WriteLine("\n\nSaving outputs...");
                    // Serialization of the complete output takes multiple hours
                    // and produces ~3.5 GB of .objml files.
                    // So we remove really large and not that interesting objects
                    // from the output before serialization by default.
                    // Feel free to comment out the next line if you're interested
                    // in complete .objml's and willing to wait.
                    TrimLargeObjects(outputter.Output);
                    outputter.SaveOutputAsProducedFlattening(args[0]);
                    Console.WriteLine("Done saving.");
                }
            }
        }

        public static void RunExperiments(Outputter outputter)
        {
            bool showFactorGraph = false;

            LoadAllInputFiles(DataPath);

            Console.WriteLine($"\n{Contents.S2AModelForClassification.NumberedName}.\n");
            var oneFeature = OneFeature.Run(showFactorGraph, ThresholdAndNoiseVariance);
            outputter.Out(oneFeature, Contents.S2AModelForClassification.NumberedName, "OneFeature");

            Console.WriteLine("Producing demo for step and Gaussian CDFs.");
            var stepAndGaussianCdfDemo = StepAndGaussianCdfDemo();
            outputter.Out(stepAndGaussianCdfDemo, Contents.S2AModelForClassification.NumberedName, "StepAndGaussianCdfDemo");


            Console.WriteLine($"\n{Contents.S3ModellingMultipleFeatures.NumberedName}.\n");
            Console.WriteLine("Producing demo for logistic function.");
            var logisticDemo = LogisticDemo();
            outputter.Out(logisticDemo, Contents.S3ModellingMultipleFeatures.NumberedName, "LogisticFunctionDemo");

            var combiningFeatures = CombiningFeatures.Run(showFactorGraph, ThresholdAndNoiseVariance);
            outputter.Out(combiningFeatures, Contents.S3ModellingMultipleFeatures.NumberedName, "CombiningFeatures");

            Console.WriteLine($"\n{Contents.S4DesigningAFeatureSet.NumberedName}.\n");
            var featuresWithManyStates = FeaturesWithManyStates.Run(ThresholdAndNoiseVariance);
            outputter.Out(featuresWithManyStates, Contents.S4DesigningAFeatureSet.NumberedName, "FeaturesWithManyStates");

            Console.WriteLine($"\n{Contents.S5EvaluatingAndImprovingTheFeatureSet.NumberedName}.\n");
            var singleUserOffline = SingleUserOffline.Run(showFactorGraph, ThresholdAndNoiseVariance);
            outputter.Out(singleUserOffline, Contents.S5EvaluatingAndImprovingTheFeatureSet.NumberedName, "SingleUserOffline");

#if NETFULL
            try
            {
                UserMailAnalyzer userMailAnalyzer = new UserMailAnalyzer
                {
                    Months = DefaultMonths,
                    Anonymize = Anonymize.DoNotAnonymize,
                    EndTime = DateTime.Now.AddDays(1)
                };
                userMailAnalyzer.LoadUserFromDesktopSearchAndRun(showFactorGraph, ThresholdAndNoiseVariance);
                outputter.Out(userMailAnalyzer.Inbox, Contents.S5EvaluatingAndImprovingTheFeatureSet.NumberedName, "EmailClientMockup");
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nCouldn't run models on user's emails - an exception was thrown:\n{e}");
            }
#endif

            Console.WriteLine($"\n{Contents.S6LearningAsEmailsArrive.NumberedName}.\n");
            var singleUserOnline = SingleUserOnline.Run(showFactorGraph, ThresholdAndNoiseVariance);
            outputter.Out(singleUserOnline, Contents.S6LearningAsEmailsArrive.NumberedName, "SingleUserOnline");

            Console.WriteLine("Producing demo for Gamma PDFs.");
            var gammasDemo = GammasDemo();
            outputter.Out(gammasDemo, Contents.S6LearningAsEmailsArrive.NumberedName, "GammasDemo");

            var communityOnline = CommunityOnline.Run(showFactorGraph, ThresholdAndNoiseVariance);
            outputter.Out(communityOnline, Contents.S6LearningAsEmailsArrive.NumberedName, "CommunityOnline");


            Console.WriteLine("\nCompleted all experiments.");
        }

        private static void TrimLargeObjects(IDictionary<string, object> variableDictionary)
        {
            var toReplace = new List<KeyValuePair<string, object>>();
            foreach (var kvp in variableDictionary)
            {
                if (kvp.Value is IDictionary<string, object> dict)
                    TrimLargeObjects(dict);
                else if (kvp.Value is Trial trial)
                    toReplace.Add(new KeyValuePair<string, object>(kvp.Key, $"## Trial {trial.Name} trimmed ##"));
                else if (kvp.Value is Dictionary<FeatureSetType, Trial>)
                    toReplace.Add(new KeyValuePair<string, object>(kvp.Key, $"## Trial collection trimmed ##"));
                else if (kvp.Value is Dictionary<int, Trial>)
                    toReplace.Add(new KeyValuePair<string, object>(kvp.Key, $"## Trial collection trimmed ##"));
                else if (kvp.Value is InputsCollection)
                    toReplace.Add(new KeyValuePair<string, object>(kvp.Key, "## Inputs collection trimmed ##"));
                else if (kvp.Value is ExperimentCollection ec)
                    toReplace.Add(new KeyValuePair<string, object>(kvp.Key, $"## Experiment collection {ec.Name} trimmed ##"));
#if NETFULL
                else if (kvp.Value is Views.InboxViewModel)
                    toReplace.Add(new KeyValuePair<string, object>(kvp.Key, $"## User's email data trimmed ##"));
#endif
            }
            foreach (var kvp in toReplace)
                variableDictionary[kvp.Key] = kvp.Value;
        }
        
        /// <summary>
        /// Initializes UI.
        /// </summary>
        private static void InitializeUI()
        {
#if NETFULL
            // Initialise MBMLViews/Glo
            var t = new[]
                        {
                            typeof(MatrixCanvasViewModel), 
                            typeof(double[][]), 
                            typeof(IList<Vector>), 
                            typeof(IList<SparseVector>), 
                            typeof(Matrix), 
                            typeof(Inputs),
                            typeof(User),
                            typeof(Experiment),
                            typeof(ExperimentCollection),
                            typeof(IEnumerable<KeyValuePair<string, Gaussian>>),
                            typeof(Percentage),
                            typeof(PointWithBounds)
                        };
            Microsoft.ML.Probabilistic.Models.InferenceEngine.Visualizer = new Microsoft.ML.Probabilistic.Compiler.Visualizers.WindowsVisualizer();
#endif
        }
    }
}
