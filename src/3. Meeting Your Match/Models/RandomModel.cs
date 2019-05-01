// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Models
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Math;

    using global::MeetingYourMatch.Experiments;
    using global::MeetingYourMatch.Items;

    /// <summary>
    /// The random model.
    /// </summary>
    public class RandomModel : IModel, IHasParameters<RandomModelParameters>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RandomModel" /> class.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        public RandomModel(IModelParameters parameters)
        {
            this.Parameters = (RandomModelParameters)parameters;
            this.OutcomeDistribution = new Discrete(
                1 - this.Parameters.EmpiricalDrawProportion,
                this.Parameters.EmpiricalDrawProportion,
                1 - this.Parameters.EmpiricalDrawProportion);
        }

        /// <summary>
        /// Gets or sets the outcome distribution.
        /// </summary>
        public Discrete OutcomeDistribution { get; set; }

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        public RandomModelParameters Parameters { get; set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get
            {
                return "Random";
            }
        }

        /// <summary>
        /// Trains the specified game.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="players">The players.</param>
        /// <param name="priors">The priors.</param>
        /// <returns>
        /// The <see cref="Results" />.
        /// </returns>
        public Results Train(Game game, IList<string> players, Marginals priors)
        {
            return new Results { Posteriors = priors };
        }

        /// <summary>
        /// Trains the specified games.
        /// </summary>
        /// <param name="games">The games.</param>
        /// <param name="players">The players.</param>
        /// <param name="priors">The priors.</param>
        /// <returns>
        /// The <see cref="IList{Results}" />.
        /// </returns>
        public IList<Results> Train(IList<Game> games, IList<string> players, Marginals priors)
        {
            return games.Select(ia => new Results { Posteriors = priors }).ToList();
        }

        /// <summary>
        /// Predicts the outcome of a game.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="posteriors">The posteriors.</param>
        /// <returns>
        /// The <see cref="Prediction" />.
        /// </returns>
        public Prediction PredictOutcome(Game game, Marginals posteriors)
        {
            var twoPlayer = game as TwoPlayerGame;
            if (twoPlayer != null)
            {
                return this.PredictOutcome(twoPlayer, posteriors);
            }

            var teamGame = game as TeamGame;
            if (teamGame != null)
            {
                return this.PredictOutcome(teamGame, posteriors);
            }

            // Unsupported game type
            return null;
        }

        /// <summary>
        /// Predicts the outcome of a game.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="posteriors">The posteriors.</param>
        /// <returns>
        /// The <see cref="TwoPlayerPrediction" />.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">Multi-player/team games not supported</exception>
        public TwoPlayerPrediction PredictOutcome(TwoPlayerGame game, Marginals posteriors)
        {
            return new TwoPlayerPrediction
                       {
                           Actual = game.Outcome,
                           Predicted =
                               this.Parameters.IncludeDraws
                                   ? (MatchOutcome)this.OutcomeDistribution.Sample()
                                   : (Rand.Int(2) == 0 ? MatchOutcome.Player1Win : MatchOutcome.Player2Win),
                           IncludeDraws = this.Parameters.IncludeDraws
                       };
        }

        /// <summary>
        /// Predicts the outcome of a game.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="posteriors">The posteriors.</param>
        /// <returns>
        /// The <see cref="TwoTeamPrediction" />.
        /// </returns>
        public TwoTeamPrediction PredictOutcome(TeamGame game, Marginals posteriors)
        {
            return new TwoTeamPrediction
            {
                Actual = game.Outcome,
                Predicted =
                    this.Parameters.IncludeDraws
                        ? (TeamMatchOutcome)this.OutcomeDistribution.Sample()
                        : (Rand.Int(2) == 0 ? TeamMatchOutcome.Team1Win : TeamMatchOutcome.Team2Win),
                IncludeDraws = this.Parameters.IncludeDraws
            };
        }
    }
}
