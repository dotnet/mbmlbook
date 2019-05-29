// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using UnclutteringYourInbox.Features;

    using MBMLViews;

    using Microsoft.ML.Probabilistic.Collections;
    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Models;
    using Microsoft.ML.Probabilistic.Models.Attributes;

    /// <summary>
    /// The community model (without threshold).
    /// </summary>
    public class CommunityModel : CommunityModelBase
    {
        /// <summary>
        /// Constructs the model.
        /// </summary>
        public override void ConstructModel()
        {
            if (this.HasBeenConstructed)
            {
                return;
            }

            this.NumberOfPeople = Variable.New<int>().Named("NumberOfPeople").Attrib(new DoNotInfer());
            this.NumberOfFeatures = Variable.New<int>().Named("NumberOfFeatures").Attrib(new DoNotInfer());
            
            Range buckets = new Range(this.NumberOfFeatures).Named("buckets");
            Range users = new Range(this.NumberOfPeople).Named("users");

            this.NumberOfMessages = Variable.Array<int>(users).Named("numberOfMessages").Attrib(new DoNotInfer());

            Range emails = new Range(this.NumberOfMessages[users]).Named("emails");

            // Make sure that the range across messages is handled sequentially
            // - this is necessary to ensure the model converges during training
            emails.AddAttribute(new Sequential());

            this.FeatureCounts = Variable.Array(Variable.Array<int>(emails), users).Named("FeatureCounts").Attrib(new DoNotInfer());
            
            Range indices = new Range(this.FeatureCounts[users][emails]).Named("indices");
            
            // observed data
            this.FeatureValue =
                Variable.Array(Variable.Array(Variable.Array<double>(indices), emails), users)
                    .Named("featureValue")
                    .Attrib(new DoNotInfer());

            this.FeatureIndices =
                Variable.Array(Variable.Array(Variable.Array<int>(indices), emails), users)
                    .Named("featureIndices")
                    .Attrib(new DoNotInfer());

            // The weights
            this.Weight = Variable.Array(Variable.Array<double>(buckets), users).Named("weight");
            
            // The priors on the weights
            this.WeightMean = Variable.Array<double>(buckets).Named("weightMean");
            this.WeightPrecision = Variable.Array<double>(buckets).Named("weightPrecision");

            this.WeightMeanPriors = Variable.New<DistributionStructArray<Gaussian, double>>().Named("WeightMeanPriors").Attrib(new DoNotInfer());
            this.WeightPrecisionPriors = Variable.New<DistributionStructArray<Gamma, double>>().Named("WeightPrecisionPriors").Attrib(new DoNotInfer());
            
            this.WeightMean.SetTo(Variable<double[]>.Random(this.WeightMeanPriors));
            this.WeightPrecision.SetTo(Variable<double[]>.Random(this.WeightPrecisionPriors));

            // Noise Variance
            this.NoiseVariance = Variable.New<double>().Named("NoiseVariance").Attrib(new DoNotInfer());

            // Label: is the message replied to?
            this.RepliedTo = Variable.Array(Variable.Array<bool>(emails), users).Named("repliedTo");

            // Loop over people
            using (Variable.ForEach(users))
            {
                // Loop over features
                using (Variable.ForEach(buckets))
                {
                    this.Weight[users][buckets] = Variable.GaussianFromMeanAndPrecision(
                        this.WeightMean[buckets],
                        this.WeightPrecision[buckets]);
                }

                // Loop over emails
                using (Variable.ForEach(emails))
                {
                    var featureWeight = Variable.Subarray(this.Weight[users], this.FeatureIndices[users][emails]).Named("featureWeight");
                    var featureScore = Variable.Array<double>(indices).Named("featureScore");

                    featureScore[indices] = this.FeatureValue[users][emails][indices]
                                                           * featureWeight[indices];

                    var score = Variable.Sum(featureScore).Named("score");

                    this.RepliedTo[users][emails] = Variable.GaussianFromMeanAndVariance(score, this.NoiseVariance).Named("noisyScore") > 0;
                }
            }

            this.InitializeEngine();

            // if during personalisation we update the weights rather than the weight mean and precision, then this will need to change
            switch (this.Mode)
            {
                case InputMode.CommunityTraining:
                case InputMode.Training:
                    this.Engine.OptimiseForVariables = new IVariable[] { this.Weight, this.WeightMean, this.WeightPrecision };
                    break;
                default:
                    this.Engine.OptimiseForVariables = this.Engine.OptimiseForVariables = new IVariable[] { this.RepliedTo };
                    break;
            }

            this.HasBeenConstructed = true;
        }

        /// <summary>
        /// Sets the observed values.
        /// </summary>
        /// <param name="instances">The instances.</param>
        /// <param name="featureSet">The feature set.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="communityPriors">The community priors.</param>
        /// <exception cref="System.InvalidOperationException">Data is null</exception>
        public override void SetObservedValues(IList<IList<Inputs.Instance>> instances, FeatureSet featureSet, InputMode mode, CommunityPriors communityPriors)
        {
            // This is for personalisation training and testing
            if (instances == null)
            {
                throw new InvalidOperationException("Data is null");
            }

            this.NumberOfPeople.ObservedValue = instances.Count;
            this.NumberOfMessages.ObservedValue = instances.Select(ia => ia.Count).ToArray();

            this.NumberOfFeatures.ObservedValue = mode == InputMode.CommunityTraining
                                                      ? featureSet.SharedFeatureVectorLength
                                                      : featureSet.FeatureVectorLength;

            this.NoiseVariance.ObservedValue = communityPriors.NoiseVariance;

            if (mode == InputMode.CommunityTraining || mode == InputMode.Training)
            {
                this.RepliedTo.ObservedValue = instances.Select(inner => inner.Select(ia => ia.Label).ToArray()).ToArray();
            }

            this.WeightMeanPriors.ObservedValue = DistributionArrayHelpers.Copy(communityPriors.WeightMeans.Values);
            this.WeightPrecisionPriors.ObservedValue = DistributionArrayHelpers.Copy(communityPriors.WeightPrecisions.Values);
            
            // If this is community training, we only want shared features
            Func<KeyValuePair<FeatureBucket, double>, bool> predicate =
                fbv => mode != InputMode.CommunityTraining || fbv.Key.Feature.IsShared;

            this.FeatureIndices.ObservedValue =
                instances.Select(
                    inner =>
                    inner.Select(
                        ia => ia.FeatureValues.Where(predicate).Select(fbv => featureSet.FeatureBuckets.IndexOf(fbv.Key)).ToArray())
                        .ToArray()).ToArray();

            this.FeatureValue.ObservedValue =
                instances.Select(
                    inner => inner.Select(ia => ia.FeatureValues.Where(predicate).Select(fbv => fbv.Value).ToArray()).ToArray()).ToArray();

            this.FeatureCounts.ObservedValue =
                this.FeatureValue.ObservedValue.Select(inner => inner.Select(ia => ia.Length).ToArray()).ToArray();
        }

        /// <summary>
        /// Does the inference.
        /// </summary>
        /// <param name="featureSet">The feature Set.</param>
        /// <param name="userNames">The user names.</param>
        /// <param name="results">The results.</param>
        public override void DoInference(FeatureSet featureSet, IEnumerable<string> userNames, ref Results results)
        {
            var means = this.Engine.Infer<IEnumerable<Gaussian>>(this.WeightMean);
            var precisions = this.Engine.Infer<IEnumerable<Gamma>>(this.WeightPrecision);
            var buckets = featureSet.FeatureBuckets;

            Func<FeatureBucket, Gaussian, KeyValuePair<FeatureBucket, Gaussian>> toDict1 =
                (b, g) => new KeyValuePair<FeatureBucket, Gaussian>(b, g);
            Func<FeatureBucket, Gamma, KeyValuePair<FeatureBucket, Gamma>> toDict2 =
                (b, g) => new KeyValuePair<FeatureBucket, Gamma>(b, g);

            if (this.Mode == InputMode.CommunityTraining || this.Mode == InputMode.Training)
            {
                results.Posteriors = new Posteriors
                                         {
                                             Weights =
                                                 buckets.Zip(
                                                     this.Engine.Infer<Gaussian[][]>(this.Weight).First(),
                                                     toDict1).ToDictionary(),
                                         };
            }

            // no need to infer thresholds in community training as they won't get used
            results.CommunityPosteriors = new CommunityPosteriors
                                              {
                                                  WeightMeans = buckets.Zip(means, toDict1).ToDictionary(),
                                                  WeightPrecisions = buckets.Zip(precisions, toDict2).ToDictionary(),
                                              };
        }
        
        /// <summary>
        /// Initializes the engine.
        /// </summary>
        public void InitializeEngine()
        {
            this.Engine = new InferenceEngine
            {
                ShowFactorGraph = this.ShowFactorGraph,
                ShowProgress = false,
                ShowMsl = false,
                ShowSchedule = false,
                ShowTimings = false,
                ShowWarnings = false,
                NumberOfIterations = this.Mode == InputMode.Testing ? 1 : 10,
            };

            this.Engine.Compiler.UseSerialSchedules = true;
            this.Engine.Compiler.ReturnCopies = false;
        }
    }
}