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
    /// The community model base.
    /// </summary>
    public abstract class CommunityModelBase
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
        /// Gets or sets the is replied to.
        /// </summary>
        protected VariableArray<VariableArray<bool>, bool[][]> RepliedTo { get; set; }

        /// <summary>
        /// Gets or sets the number of messages.
        /// </summary>
        protected VariableArray<int> NumberOfMessages { get; set; }

        /// <summary>
        /// Gets or sets the number of people.
        /// </summary>
        protected Variable<int> NumberOfPeople { get; set; }

        /// <summary>
        /// Gets or sets the number of features.
        /// </summary>
        protected Variable<int> NumberOfFeatures { get; set; }

        /// <summary>
        /// Gets or sets the feature indices.
        /// </summary>
        protected VariableArray<VariableArray<VariableArray<int>, int[][]>, int[][][]> FeatureIndices { get; set; }

        /// <summary>
        /// Gets or sets the feature values.
        /// </summary>
        protected VariableArray<VariableArray<VariableArray<double>, double[][]>, double[][][]> FeatureValue { get; set; }

        /// <summary>
        /// Gets or sets the feature counts.
        /// </summary>
        protected VariableArray<VariableArray<int>, int[][]> FeatureCounts { get; set; }

        /// <summary>
        /// Gets or sets the weight prior means.
        /// </summary>
        protected VariableArray<double> WeightMean { get; set; }

        /// <summary>
        /// Gets or sets the weight mean priors.
        /// </summary>
        protected Variable<DistributionStructArray<Gaussian, double>> WeightMeanPriors { get; set; }

        /// <summary>
        /// Gets or sets the weight prior precisions.
        /// </summary>
        protected VariableArray<double> WeightPrecision { get; set; }

        /// <summary>
        /// Gets or sets the weight precision priors.
        /// </summary>
        protected Variable<DistributionStructArray<Gamma, double>> WeightPrecisionPriors { get; set; }

        /// <summary>
        /// Gets or sets the weights.
        /// </summary>
        protected VariableArray<VariableArray<double>, double[][]> Weight { get; set; }

        #endregion

        /// <summary>
        /// Constructs the model.
        /// </summary>
        public abstract void ConstructModel();

        /// <summary>
        /// Sets the observed values.
        /// </summary>
        /// <param name="instances">The instances.</param>
        /// <param name="featureSet">The feature set.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="communityPriors">The community priors.</param>
        public abstract void SetObservedValues(IList<IList<Inputs.Instance>> instances, FeatureSet featureSet, InputMode mode, CommunityPriors communityPriors);

        /// <summary>
        /// Does the inference.
        /// </summary>
        /// <param name="featureSet">The feature set.</param>
        /// <param name="userNames">The user names.</param>
        /// <param name="results">The results.</param>
        public abstract void DoInference(FeatureSet featureSet, IEnumerable<string> userNames, ref Results results);

        /// <summary>
        /// Applies the specified inputs.
        /// </summary>
        /// <param name="inputs">The inputs.</param>
        /// <param name="featureSet">The feature set.</param>
        /// <param name="priors">The priors.</param>
        /// <param name="results">The results.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="showFactorGraph">if set to <c>true</c> [show factor graph].</param>
        public void Apply(Inputs inputs, FeatureSet featureSet, CommunityPriors priors, ref Results results, InputMode mode, bool showFactorGraph = false)
        {
            this.Engine.ShowFactorGraph = showFactorGraph;

            if (mode.HasFlag(InputMode.Validation))
            {
                this.SetObservedValues(new[] { inputs.Validation.Instances }, featureSet, InputMode.Validation, priors);
                results.Validation = new Results.ResultsSet { IsRepliedTo = this.Engine.Infer<Bernoulli[][]>(this.RepliedTo)[0] };
            }

            if (mode.HasFlag(InputMode.Testing))
            {
                this.SetObservedValues(new[] { inputs.Test.Instances }, featureSet, InputMode.Testing, priors);
                results.Test = new Results.ResultsSet { IsRepliedTo = this.Engine.Infer<Bernoulli[][]>(this.RepliedTo)[0] };
            }

            this.Engine.ShowFactorGraph = this.ShowFactorGraph;
        }
    }
}
