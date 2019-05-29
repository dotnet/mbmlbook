// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using UnclutteringYourInbox.Features;

    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Models;
    using Microsoft.ML.Probabilistic.Models.Attributes;

    /// <summary>
    /// The one feature model (without noise).
    /// </summary>
    public class OneFeatureNoNoiseModel : ReplyToModelBase
    {
        #region Features
        /// <summary>
        /// Gets or sets the weight priors.
        /// </summary>
        protected Variable<Gaussian> WeightPrior { get; set; }

        /// <summary>
        /// Gets or sets the weight.
        /// </summary>
        protected Variable<double> Weight { get; set; }

        /// <summary>
        /// Gets or sets the feature values.
        /// </summary>
        protected VariableArray<double> FeatureValue { get; set; }
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

            this.NumberOfMessages = Variable.New<int>().Named("NumberOfMessages").Attrib(new DoNotInfer());
            Range emails = new Range(this.NumberOfMessages).Named("emails");

            // Make sure that the range across messages is handled sequentially
            // - this is necessary to ensure the model converges during training
            emails.AddAttribute(new Sequential());

            // observed data
            this.FeatureValue = Variable.Array<double>(emails).Named("featureValue").Attrib(new DoNotInfer());

            // The weight
            this.WeightPrior = Variable.New<Gaussian>().Named("WeightPrior").Attrib(new DoNotInfer());
            this.Weight = Variable.New<double>().Named("weight");
            this.Weight.SetTo(Variable<double>.Random(this.WeightPrior));

            // The threshold
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
                var score = (this.FeatureValue[emails] * this.Weight).Named("score");
                this.RepliedTo[emails] = score > this.Threshold;
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
            this.FeatureValue.ClearObservedValue();
            this.WeightPrior.ClearObservedValue();
            this.ThresholdPrior.ClearObservedValue();
            this.NoiseVariance.ClearObservedValue();
            this.RepliedTo.ClearObservedValue();
        }

        /// <summary>
        /// Sets the observed values.
        /// </summary>
        /// <param name="instances">The instances.</param>
        /// <param name="featureSet">The feature buckets.</param>
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
            this.FeatureValue.ObservedValue = instances.Select(ia => ia.FeatureValues.First().Value).ToArray();
            this.NoiseVariance.ObservedValue = priors.NoiseVariance;
            
            if (mode == InputMode.Training)
            {
                // No priors - this is training
                this.RepliedTo.ObservedValue = instances.Select(ia => ia.Label).ToArray();
                this.WeightPrior.ObservedValue = priors.Weights.First().Value;
                this.ThresholdPrior.ObservedValue = priors.Threshold;
            }
            else
            {
                // This is testing, don't set isRepliedTo observed values
                this.RepliedTo.ClearObservedValue();
                this.WeightPrior.ObservedValue = priors.Weights.First().Value;
                this.ThresholdPrior.ObservedValue = priors.Threshold;
            }
        }

        /// <summary>
        /// Documents the inference.
        /// </summary>
        /// <param name="featureSet">The feature set.</param>
        /// <param name="results">The results.</param>
        public override void DoInference(FeatureSet featureSet, ref Results results)
        {
            results.Posteriors = new Posteriors
                    {
                        Weights = new Dictionary<FeatureBucket, Gaussian>
                                      {
                                          {
                                              featureSet.FeatureBuckets.First(), 
                                              this.Engine.Infer<Gaussian>(this.Weight)
                                          }
                                      },
                        Threshold = this.Engine.Infer<Gaussian>(this.Threshold) 
                    };
        }
    }
}
