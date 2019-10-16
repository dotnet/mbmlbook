// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace HarnessingTheCrowd
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    using Microsoft.ML.Probabilistic.Distributions;

    /// <summary>
    /// The biased community model runner.
    /// </summary>
    public class BiasedCommunityModelRunner : ModelRunnerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BiasedCommunityModelRunner"/> class. 
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
        public BiasedCommunityModelRunner(CrowdDataMapping dataMapping, BiasedCommunityModel model, ModelRunnerBase trainingRunner = null)
            : base(dataMapping, model, trainingRunner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BiasedCommunityModelRunner"/> class.
        /// </summary>
        public BiasedCommunityModelRunner()
        {
        }

        /// <summary>
        /// Gets or sets the posterior of the conditional probability table of each community.
        /// </summary>
        [DataMember]
        public List<Dirichlet[]> CommunityCpt { get; set; }

        /// <summary>
        /// Gets or sets the posterior of the worker communities.
        /// </summary>
        [DataMember]
        public Dictionary<string, int> WorkerCommunities { get; set; }

        /// <summary>
        /// Gets or sets the posterior of the counts of each community.
        /// </summary>
        [DataMember]
        public List<int> WorkerCommunityCounts { get; set; }

        /// <inheritdoc />
        protected override void ClearResults()
        {
            base.ClearResults();
            this.CommunityCpt = new List<Dirichlet[]>();
            this.WorkerCommunities = new Dictionary<string, int>();
            this.WorkerCommunityCounts = new List<int>();
        }

        /// <inheritdoc />
        protected override void UpdateResults()
        {
            var modelPosteriors = this.Posteriors as BiasedCommunityModel.BiasedCommunityModelPosteriors;
            this.CommunityCpt = modelPosteriors?.CommunityCpt.ToList();
            this.WorkerCommunities = modelPosteriors?.WorkerCommunities.Select((disc, w) => new { w, disc })
                .ToDictionary(pr => this.DataMapping.WorkerIndexToId[pr.w], pr => pr.disc.GetMode());
            this.WorkerCommunityCounts = this.WorkerCommunities?.GroupBy(wc => wc.Value).OrderByDescending(grp => grp.Key)
                .Select(grp => grp.Count()).ToList();
            base.UpdateResults();
        }
    }
}
