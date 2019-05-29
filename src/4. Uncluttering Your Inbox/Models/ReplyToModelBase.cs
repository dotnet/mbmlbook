// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Models
{
    using System.Collections.Generic;

    using UnclutteringYourInbox.Features;

    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Models;

    /// <summary>
    /// The reply to model base class.
    /// </summary>
    public abstract class ReplyToModelBase
    {
        /// <summary>
        /// Gets or sets the mode (Train or validation/test).
        /// </summary>
        public InputMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the engine.
        /// </summary>
        public InferenceEngine Engine { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether has been constructed.
        /// </summary>
        public bool HasBeenConstructed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show the factor graph.
        /// </summary>
        public bool ShowFactorGraph { get; set; }

        #region Variables
        /// <summary>
        /// Gets or sets the noise prior variance.
        /// </summary>
        protected Variable<double> NoiseVariance { get; set; }

        /// <summary>
        /// Gets or sets threshold prior.
        /// </summary>
        protected Variable<Gaussian> ThresholdPrior { get; set; }

        /// <summary>
        /// Gets or sets the is replied to.
        /// </summary>
        protected VariableArray<bool> RepliedTo { get; set; }

        /// <summary>
        /// Gets or sets the number of messages.
        /// </summary>
        protected Variable<int> NumberOfMessages { get; set; }

        /// <summary>
        /// Gets or sets the threshold.
        /// </summary>
        protected Variable<double> Threshold { get; set; }
        #endregion

        /// <summary>
        /// Samples from model.
        /// </summary>
        /// <param name="inputs">The inputs.</param>
        /// <param name="numberOfSamples">The number of samples.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The inputs</returns>
        public abstract Inputs SampleFromModel(Inputs inputs, int numberOfSamples, params object[] parameters);

        /// <summary>
        /// Constructs the model.
        /// </summary>
        public abstract void ConstructModel();

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
                NumberOfIterations = this.Mode == InputMode.Training ? 10 : 1,
            };

            this.Engine.Compiler.UseSerialSchedules = true;
            this.Engine.Compiler.ReturnCopies = false;
        }

        /// <summary>
        /// Clears the observed variables.
        /// </summary>
        public abstract void ClearObservedVariables();

        /// <summary>
        /// Sets the observed values.
        /// </summary>
        /// <param name="instances">The instances.</param>
        /// <param name="featureSet">The feature buckets.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="priors">The priors.</param>
        public abstract void SetObservedValues(IList<Inputs.Instance> instances, FeatureSet featureSet, InputMode mode, Priors priors);

        /// <summary>
        /// Does the inference.
        /// </summary>
        /// <param name="featureSet">The feature set.</param>
        /// <param name="results">The results.</param>
        public abstract void DoInference(FeatureSet featureSet, ref Results results);
        
        /// <summary>
        /// Applies the specified inputs.
        /// </summary>
        /// <param name="inputs">The inputs.</param>
        /// <param name="featureBuckets">The feature buckets.</param>
        /// <param name="priors">The priors.</param>
        /// <param name="results">The results.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="showFactorGraph">if set to <c>true</c> [show factor graph].</param>
        /// <exception cref="System.NotImplementedException">Not implemented for this model</exception>
        public virtual void Apply(Inputs inputs, FeatureSet featureBuckets, Priors priors, ref Results results, InputMode mode, bool showFactorGraph = false)
        {
            this.Engine.ShowFactorGraph = showFactorGraph;

            if (mode.HasFlag(InputMode.Validation))
            {
                this.SetObservedValues(inputs.Validation.Instances, featureBuckets, InputMode.Validation, priors);
                results.Validation = new Results.ResultsSet { IsRepliedTo = this.Engine.Infer<Bernoulli[]>(this.RepliedTo) };
            }

            if (mode.HasFlag(InputMode.Testing))
            {
                this.SetObservedValues(inputs.Test.Instances, featureBuckets, InputMode.Testing, priors);
                results.Test = new Results.ResultsSet { IsRepliedTo = this.Engine.Infer<Bernoulli[]>(this.RepliedTo) };
            }

            if (mode.HasFlag(InputMode.TrainAndValidation))
            {
                this.SetObservedValues(inputs.TrainAndValidation.Instances, featureBuckets, InputMode.TrainAndValidation, priors);
                results.Validation = new Results.ResultsSet { IsRepliedTo = this.Engine.Infer<Bernoulli[]>(this.RepliedTo) };
            }

            this.Engine.ShowFactorGraph = this.ShowFactorGraph;
        }
    }
}
