// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace HarnessingTheCrowd
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Microsoft.ML.Probabilistic.Distributions;

    /// <summary>
    /// The honest worker model runner.
    /// </summary>
    [Serializable]
    [DataContract]
    public class HonestWorkerRunner : ModelRunnerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HonestWorkerRunner"/> class. 
        /// </summary>
        /// <param name="dataMapping">
        /// The data mapping.
        /// </param>
        /// <param name="model">
        /// The model.
        /// </param>
        /// <param name="trainingRunner">
        /// The training runner.
        /// </param>
        public HonestWorkerRunner(CrowdDataMapping dataMapping, HonestWorkerModel model, ModelRunnerBase trainingRunner = null)
            : base(dataMapping, model, trainingRunner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HonestWorkerRunner"/> class.
        /// </summary>
        public HonestWorkerRunner()
        {
        }

        /// <summary>
        /// Gets or sets the distributions over worker abilities.
        /// </summary>
        [DataMember]
        public Dictionary<string, Beta> WorkerAbility { get; set; }

        /// <summary>
        /// Gets or sets the distribution over the probability vector for random guesses.
        /// </summary>
        [DataMember]
        public Dirichlet ProbRandomGuess { get; set; }

        /// <summary>
        /// Gets or sets the number of worker confusion matrices to include in the written results.
        /// </summary>
        public int NumberWorkerAbilitiesToIncludeInResults { get; set; } = 10;

        /// <inheritdoc />
        protected override void ClearResults()
        {
            base.ClearResults();
            this.ProbRandomGuess = Dirichlet.Uniform(this.DataMapping.LabelCount);
            this.WorkerAbility = new Dictionary<string, Beta>();
        }

        /// <inheritdoc />
        protected override void UpdateResults()
        {
            var honestWorkerPosteriors = this.Posteriors as HonestWorkerModel.HonestWorkerModelPosteriors;
            if (honestWorkerPosteriors?.WorkerAbility != null)
            {
                for (var w = 0; w < honestWorkerPosteriors.WorkerAbility.Length; w++)
                {
                    this.WorkerAbility[this.DataMapping.WorkerIndexToId[w]] =
                        honestWorkerPosteriors.WorkerAbility[w];
                }

                this.ProbRandomGuess = honestWorkerPosteriors.RandomGuessProbability;
            }

            base.UpdateResults();
        }
    }
}
