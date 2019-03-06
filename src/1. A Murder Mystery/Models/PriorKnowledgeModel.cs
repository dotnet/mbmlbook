// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using Microsoft.ML.Probabilistic.Models;

namespace MurderMystery
{
    /// <summary>
    /// Model that contains only prior probabilities. Base class for more complex models.
    /// </summary>
    class PriorKnowledgeModel
    {
        /// <summary>
        /// Engine used to perform the inference on this model.
        /// Exposed so, that the caller could configure the process of inference.
        /// E.g. select the inference algorithm, ask factor graphs to be shown.
        /// </summary>
        public InferenceEngine Engine { get; set; }

        /// <summary>
        /// Name of the model
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Prior probabilities for Grey and Auburn being the murderer
        /// </summary>
        public MurdererProbs Priors { get; set; }

        /// <summary>
        /// Posterior probabilities for Grey and Auburn being the murderer
        /// </summary>
        public MurdererProbs Posteriors { get; protected set; }

        /// <summary>
        /// Infer.Net model's variable. Probability of Auburn being the murderer.
        /// Probability for murderer being Grey is (1 - murderer)
        /// </summary>
        protected Variable<bool> murderer;

        protected virtual void ConstructModel()
        {
            if (Priors == null)
                throw new InvalidOperationException($"{nameof(Priors)} cannot be null.");
            murderer = Variable.Bernoulli(Priors.Auburn).Named("murderer=Auburn");
        }

        protected virtual void ComputePosteriors() { }

        protected virtual Variables Variables
        {
            get
            {
                return new Variables
                {
                    MurdererMarginals = Priors,
                    Name = Name,
                    Posteriors = Posteriors
                };
            }
        }

        public Variables DoInference()
        {
            ConstructModel();
            ComputePosteriors();
            return Variables;
        }
    }
}
