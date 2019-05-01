// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch
{
    using System.Collections.Generic;
    using System.Linq;
    using MBMLCommon;

    using Microsoft.ML.Probabilistic.Distributions;

    using MeetingYourMatch.Items;
    using MeetingYourMatch.Models;

    /// <summary>
    /// The toy experiment. 
    /// This is for the purposes of running an experiment for a single game only.
    /// The summary table property produces a table of results.
    /// </summary>
    public class ToyExperiment
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToyExperiment" /> class.
        /// </summary>
        public ToyExperiment()
        {
            // Default constructor for the purposes of serialization only
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToyExperiment" /> class.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="game">The game.</param>
        /// <param name="priors">The priors.</param>
        public ToyExperiment(IModel model, Game game, Marginals priors)
        {
            this.Model = model;
            this.Game = game;
            this.Priors = priors;
            this.Posteriors = this.Model.Train(this.Game, this.Game.Players, this.Priors).Posteriors;
        }

        /// <summary>
        /// Gets the model.
        /// </summary>
        public IModel Model { get; private set; }

        /// <summary>
        /// Gets or sets the game.
        /// </summary>
        public Game Game { get; set; }

        /// <summary>
        /// Gets or sets the priors.
        /// </summary>
        public Marginals Priors { get; set; }

        /// <summary>
        /// Gets or sets the posteriors.
        /// </summary>
        public Marginals Posteriors { get; set; }

        /// <summary>
        /// Gets the summary.
        /// </summary>
        public Dictionary<string, Dictionary<DistributionType, Gaussian>> Summary
        {
            get
            {
                var dict = new Dictionary<string, Dictionary<DistributionType, Gaussian>>();
                foreach (var kvp in this.Priors.Skills.Where(kvp => this.Posteriors.Skills.ContainsKey(kvp.Key)))
                {
                    dict[kvp.Key] = new Dictionary<DistributionType, Gaussian>
                                        {
                                            { DistributionType.Prior, kvp.Value },
                                            {
                                                DistributionType.Posterior,
                                                this.Posteriors.Skills[kvp.Key]
                                            }
                                        };
                }

                return dict;
            }
        }

        /// <summary>
        /// Gets the summary table.
        /// </summary>
        public Dictionary<string, object> SummaryTable
        {
            get
            {
                var summaryTable = new Dictionary<string, object>();
                summaryTable["Player"] = this.Game.Players;
                summaryTable["Before"] = this.Priors.Skills.Values.Select(Utils.GetMean);
                summaryTable["After"] = this.Posteriors.Skills.Values.Select(Utils.GetMean);
                return summaryTable;
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("Model: {0}\nGame: {1}\nPriors: {2}\nPosteriors: {3}", this.Model, this.Game, this.Priors, this.Posteriors);
        }
    }
}