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
    /// The reply to model.
    /// </summary>
    public class ReplyToModel : ReplyToModelBase
    {
        #region Variables
        /// <summary>
        /// Gets or sets the weight priors.
        /// </summary>
        protected Variable<DistributionStructArray<Gaussian, double>> WeightPriors { get; set; }

        /// <summary>
        /// Gets or sets the weights.
        /// </summary>
        protected VariableArray<double> Weight { get; set; }

        /// <summary>
        /// Gets or sets the number of features.
        /// </summary>
        protected Variable<int> NumberOfFeatures { get; set; }

        /// <summary>
        /// Gets or sets the feature values.
        /// </summary>
        protected VariableArray<VariableArray<double>, double[][]> FeatureValue { get; set; }
        #endregion

        /// <summary>
        /// Samples from model.
        /// </summary>
        /// <param name="inputs">The inputs.</param>
        /// <param name="numberOfSamples">The number of samples.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>
        /// The inputs
        /// </returns>
        public override Inputs SampleFromModel(Inputs inputs, int numberOfSamples, params object[] parameters)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Constructs the model.
        /// </summary>
        public override void ConstructModel()
        {
            if (this.HasBeenConstructed)
            {
                return;
            }
            
            this.NumberOfFeatures = Variable.New<int>().Named("numberOfFeatures").Attrib(new DoNotInfer());
            this.NumberOfMessages = Variable.New<int>().Named("numberOfMessages").Attrib(new DoNotInfer());

            Range features = new Range(this.NumberOfFeatures).Named("features");
            Range emails = new Range(this.NumberOfMessages).Named("emails");

            // Make sure that the range across messages is handled sequentially
            // - this is necessary to ensure the model converges during training
            emails.AddAttribute(new Sequential());
                
            // observed data
            this.FeatureValue =
                Variable.Array(Variable.Array<double>(features), emails).Named("featureValue").Attrib(new DoNotInfer());

            // The weights
            this.WeightPriors = Variable.New<DistributionStructArray<Gaussian, double>>().Named("WeightPriors").Attrib(new DoNotInfer());
            this.Weight = Variable.Array<double>(features).Named("weight");
            this.Weight.SetTo(Variable<double[]>.Random(this.WeightPriors));

            this.ThresholdPrior = Variable.New<Gaussian>().Named("ThresholdPrior").Attrib(new DoNotInfer());
            this.Threshold = Variable.New<double>().Named("threshold");
            this.Threshold.SetTo(Variable<double>.Random(this.ThresholdPrior));

            // Noise Variance
            this.NoiseVariance = Variable.New<double>().Named("NoiseVariance").Attrib(new DoNotInfer());

            // Label: is the message replied to?
            this.RepliedTo = Variable.Array<bool>(emails).Named("repliedTo");

            // Loop over emails
            using (Variable.ForEach(emails))
            {
                var featureScore = Variable.Array<double>(features).Named("featureScore");
                featureScore[features] = this.FeatureValue[emails][features] * this.Weight[features];
                var score = Variable.Sum(featureScore).Named("score");

                this.RepliedTo[emails] = Variable.GaussianFromMeanAndVariance(score, this.NoiseVariance).Named("noisyScore") > this.Threshold;
            }

            this.InitializeEngine();

            this.Engine.OptimiseForVariables = this.Mode == InputMode.Training
                                                   ? new IVariable[] { this.Weight, this.Threshold }
                                                   : this.Engine.OptimiseForVariables = new IVariable[] { this.RepliedTo };
            
            this.HasBeenConstructed = true;
        }

        /// <summary>
        /// Clears the observed this.
        /// </summary>
        public override void ClearObservedVariables()
        {
            this.NumberOfMessages.ClearObservedValue();
            this.NumberOfFeatures.ClearObservedValue();
            this.FeatureValue.ClearObservedValue();
            this.WeightPriors.ClearObservedValue();
            this.ThresholdPrior.ClearObservedValue();
            this.RepliedTo.ClearObservedValue();
        }

        /// <summary>
        /// Sets the observed values.
        /// </summary>
        /// <param name="instances">The instances.</param>
        /// <param name="featureSet">The feature set.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="priors">The priors.</param>
        /// <exception cref="System.InvalidOperationException">Data is null</exception>
        public override void SetObservedValues(IList<Inputs.Instance> instances, FeatureSet featureSet, InputMode mode, Priors priors)
        {
            if (instances == null)
            {
                throw new InvalidOperationException("Data is null");
            }
            
            this.NumberOfMessages.ObservedValue = instances.Count;
            this.NumberOfFeatures.ObservedValue = featureSet.FeatureVectorLength;

            this.NoiseVariance.ObservedValue = priors.NoiseVariance;

            if (mode == InputMode.Training)
            {
                this.RepliedTo.ObservedValue = instances.Select(ia => ia.Label).ToArray();
                this.WeightPriors.ObservedValue = DistributionArrayHelpers.DistributionArray(priors.Weights.Values);
                this.ThresholdPrior.ObservedValue = priors.Threshold;
            }
            else
            {
                // This is testing, don't set isRepliedTo observed values
                this.RepliedTo.ClearObservedValue();
                this.WeightPriors.ObservedValue = DistributionArrayHelpers.Copy(priors.Weights.Values);
                this.ThresholdPrior.ObservedValue = priors.Threshold;
            }

            // Convert feature values to dense jagged array
            var featureValues = new double[instances.Count][];
            for (int i = 0; i < instances.Count; i++)
            {
                featureValues[i] = new double[featureSet.FeatureVectorLength];
                foreach (var featureBucketValue in instances[i].FeatureValues)
                {
                    int index = instances[i].FeatureSet.FeatureBuckets.IndexOf(featureBucketValue.Key);
                    if (index < 0)
                    {
                        throw new InvalidOperationException("Bucket index not found");
                    }

                    featureValues[i][index] = featureBucketValue.Value;
                }
            }

            this.FeatureValue.ObservedValue = featureValues;
        }

        /// <summary>
        /// Does the inference.
        /// </summary>
        /// <param name="featureSet">The feature set.</param>
        /// <param name="results">The results.</param>
        public override void DoInference(FeatureSet featureSet, ref Results results)
        {
            results.Posteriors = new Posteriors
                                     {
                                         Weights =
                                             featureSet.FeatureBuckets.Zip(
                                                 this.Engine.Infer<Gaussian[]>(this.Weight),
                                                 (b, g) => new KeyValuePair<FeatureBucket, Gaussian>(b, g)).ToDictionary(),
                                         Threshold = this.Engine.Infer<Gaussian>(this.Threshold)
                                     };
        }
    }
}
