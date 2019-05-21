// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Experiments
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    using Microsoft.ML.Probabilistic.Distributions;

    using MeetingYourMatch.Items;
    using MeetingYourMatch.Models;

    /// <summary>
    /// The experiment.
    /// </summary>
    public abstract class Experiment
    {
        /// <summary>
        /// The name.
        /// </summary>
        private string name;

        /// <summary>
        /// Gets or sets the players.
        /// </summary>
        public ISet<string> Players { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name
        {
            get => name ?? TrainModel?.Name ?? "Unknown experiment";
            set => name = value;
        }

        /// <summary>
        /// Gets or sets the last results.
        /// </summary>
        public Results LastResults { get; set; }

        /// <summary>
        /// Gets or sets the priors.
        /// </summary>
        public Priors Priors { get; set; }

        /// <summary>
        /// Gets or sets the player posteriors.
        /// </summary>
        public Dictionary<string, List<Gaussian>> PlayerPosteriors { get; set; }

        /// <summary>
        /// Gets the leader board.
        /// </summary>
        public Dictionary<string, double> LeaderBoard
        {
            get
            {
                return PlayerPosteriors?.Select(
                        ia => new KeyValuePair<string, double>(ia.Key, Utils.ConservativeSkill(ia.Value.Last())))
                    .OrderByDescending(x => x.Value)
                    .ToDictionary(ia => ia.Key, ia => ia.Value);
            }
        }

        /// <summary>
        /// Gets the leader board.
        /// </summary>
        public IEnumerable<LeaderBoardElement> LeaderBoardTable =>
            this.PlayerPosteriors == null
                ? null
                : from p in this.PlayerPosteriors
                let last = p.Value.Last()
                let trueSkill = Utils.ConservativeSkill(last)
                orderby trueSkill descending
                select
                    new LeaderBoardElement
                    {
                        Id = p.Key,
                        GamesPlayed = p.Value.Count,
                        SkillMean = Utils.GetMean(last),
                        TrueSkill = trueSkill
                    };

        /// <summary>
        /// Gets the leader board table top 10.
        /// </summary>
        public IEnumerable<LeaderBoardElement> LeaderBoardTableTop10 => LeaderBoardTable?.Take(10);

        /// <summary>
        /// Gets the player count.
        /// </summary>
        public int PlayerCount => PlayerPosteriors?.Count ?? 0;

        /// <summary>
        /// Gets or sets the training model.
        /// </summary>
        [XmlIgnore]
        protected IModel TrainModel { get; set; }

        /// <summary>
        /// Gets or sets the prediction model.
        /// </summary>
        [XmlIgnore]
        protected IModel PredictModel { get; set; }

        /// <summary>
        /// Runs the specified players.
        /// </summary>
        /// <param name="games">The games.</param>
        /// <param name="count">The count.</param>
        /// <param name="verbose">if set to <c>true</c> [verbose].</param>
        public abstract void Run(IEnumerable<Game> games, int count, bool verbose);

        /// <summary>
        /// The leader board element.
        /// </summary>
        public struct LeaderBoardElement
        {
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            public string Id { get; set; }

            /// <summary>
            /// Gets or sets the games played.
            /// </summary>
            public int GamesPlayed { get; set; }

            /// <summary>
            /// Gets or sets the skill mean.
            /// </summary>
            public double SkillMean { get; set; }

            /// <summary>
            /// Gets or sets the true skill.
            /// </summary>
            public double TrueSkill { get; set; }
        }
    }
}