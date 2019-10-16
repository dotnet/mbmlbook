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
    /// Runs the biased worker model.
    /// </summary>
    [Serializable]
    [DataContract]
    public class BiasedWorkerModelRunner : ModelRunnerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BiasedWorkerModelRunner"/> class. 
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
        public BiasedWorkerModelRunner(CrowdDataMapping dataMapping, BiasedWorkerModel model, ModelRunnerBase trainingRunner = null)
            : base(dataMapping, model, trainingRunner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BiasedWorkerModelRunner"/> class.
        /// </summary>
        public BiasedWorkerModelRunner()
        {
        }

        /// <summary>
        /// Gets or sets the posterior of the conditional probability table of each worker.
        /// </summary>
        [DataMember]
        public Dictionary<string, Dirichlet[]> WorkerCpt { get; set; }

        /// <inheritdoc />
        protected override void ClearResults()
        {
            base.ClearResults();
            this.WorkerCpt = new Dictionary<string, Dirichlet[]>();
        }

        /// <inheritdoc />
        protected override void UpdateResults()
        {
            var modelPosteriors = this.Posteriors as BiasedWorkerModel.BiasedWorkerModelPosteriors;
            if (modelPosteriors?.WorkerCpt != null)
            {
                for (var w = 0; w < modelPosteriors.WorkerCpt.Length; w++)
                {
                    this.WorkerCpt[this.DataMapping.WorkerIndexToId[w]] =
                        modelPosteriors.WorkerCpt[w];
                }
            }

            base.UpdateResults();
        }
    }
}