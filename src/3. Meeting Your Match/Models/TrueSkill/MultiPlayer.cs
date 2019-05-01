// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Models.TrueSkill
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Models;

    using global::MeetingYourMatch.Experiments;

    using global::MeetingYourMatch.Items;

    using GameCollection = Microsoft.Research.Glo.ObjectModel.KeyedCollectionWithFunc<string, global::MeetingYourMatch.Items.Game>;
    using Microsoft.ML.Probabilistic.Models.Attributes;

    /// <summary>
    /// The multi player.
    /// </summary>
    public class MultiPlayer : ModelBase
    {
        #region Variables
        /// <summary>
        /// The number of players.
        /// </summary>
        private readonly Variable<int> numberOfPlayers;

        /// <summary>
        /// The skill priors.
        /// </summary>
        private readonly VariableArray<Gaussian> skillPriors;

        /// <summary>
        /// The skills.
        /// </summary>
        private readonly VariableArray<double> skills;

        /// <summary>
        /// The draw margin.
        /// </summary>
        private readonly Variable<double> drawMargin;

        /// <summary>
        /// The draw margin prior.
        /// </summary>
        private readonly Variable<Gaussian> drawMarginPrior;

        /// <summary>
        /// The players per game.
        /// </summary>
        private readonly Variable<int> playersPerGame;
        
        /// <summary>
        /// The player indices.
        /// </summary>
        private readonly VariableArray<int> playerIndices;

        /// <summary>
        /// The scores.
        /// </summary>
        private readonly VariableArray<int> scores;

        /// <summary>
        /// The performances.
        /// </summary>
        private readonly VariableArray<double> performances;

        #endregion

        /// <summary>
        /// The engine.
        /// </summary>
        private readonly InferenceEngine engine;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiPlayer" /> class.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="showFactorGraph">if set to <c>true</c> [show factor graph].</param>
        /// <param name="trainingModel">if set to <c>true</c> [training model].</param>
        public MultiPlayer(IModelParameters parameters, bool showFactorGraph = false, bool trainingModel = true) : base(parameters)
        {
            //The factor graph of this model slightly differs from the one from the book 
            //because this model is generic and uses arrays to support any number of players.
            this.numberOfPlayers = Variable.New<int>().Named("numberOfPlayers").Attrib(new DoNotInfer());
            var dynamicsVariance = Variable.Observed(this.Parameters.DynamicsVariance).Named("dynamicsVariance");
            var performanceVariance = Variable.Observed(this.Parameters.PerformanceVariance).Named("performanceVariance");

            Range player = new Range(this.numberOfPlayers).Named("player");
            
            this.playersPerGame = Variable.New<int>().Named("playersPerGame").Attrib(new DoNotInfer());
            Range gamePlayer = new Range(this.playersPerGame).Named("gamePlayer");

            this.skillPriors = Variable.Array<Gaussian>(player).Named("skillPriors").Attrib(new DoNotInfer());
            this.skills = Variable.Array<double>(player).Named("skills");
            this.skills[player] = Variable.GaussianFromMeanAndVariance(Variable<double>.Random(this.skillPriors[player]), dynamicsVariance);

            this.drawMargin = Variable.New<double>().Named("drawMargin");
            this.drawMarginPrior = Variable.New<Gaussian>().Named("drawMarginPrior");
            this.drawMargin.SetTo(Variable<double>.Random(this.drawMarginPrior));
            Variable.ConstrainTrue(this.drawMargin > 0);

            this.playerIndices = Variable.Array<int>(gamePlayer).Named("playerIndices").Attrib(new DoNotInfer());
            this.performances = Variable.Array<double>(gamePlayer).Named("performances");

            var gameSkills = Variable.Subarray(this.skills, this.playerIndices).Named("gameSkills");

            if (trainingModel)
            {
                this.scores = Variable.Array<int>(gamePlayer).Named("scores").Attrib(new DoNotInfer());
            }

            using (ForEachBlock gp = Variable.ForEach(gamePlayer))
            {
                this.performances[gamePlayer] = Variable.GaussianFromMeanAndVariance(gameSkills[gamePlayer], performanceVariance);

                if (trainingModel)
                {
                    using (Variable.If(gp.Index > 0))
                    {
                        var diff = (this.performances[gp.Index - 1] - this.performances[gp.Index]).Named("diff");

                        using (Variable.If(this.scores[gp.Index - 1] == this.scores[gp.Index]))
                        {
                            Variable.ConstrainBetween(diff, -this.drawMargin, this.drawMargin);
                        }

                        using (Variable.IfNot(this.scores[gp.Index - 1] == this.scores[gp.Index]))
                        {
                            Variable.ConstrainTrue(diff > this.drawMargin);
                        }
                    }
                }
            }
            
            this.engine = Utils.GetDefaultEngine(showFactorGraph);
        }

        /// <summary>
        /// Samples the specified inputs.
        /// </summary>
        /// <param name="performanceVariance">The performance variance.</param>
        /// <param name="playerIndices">The player indices.</param>
        /// <param name="truth">The truth.</param>
        /// <param name="startIndex">The start index.</param>
        /// <returns>
        /// The <see cref="GameCollection" />.
        /// </returns>
        public static GameCollection Sample(double performanceVariance, int[][] playerIndices, Marginals truth, int startIndex = 0)
        {
            // var random = new Random(0);
            var players = truth.Skills.Keys.ToArray();
            var games = new GameCollection(ia => ia.Id);
            int count = playerIndices.Length;

            for (int i = 0; i < count; i++)
            {
                // Because this model relies on the fact that the game scores are provided in descending order, 
                // we need to randomise the player order
                var randomIndices = playerIndices[i].OrderBy(ia => Guid.NewGuid()).ToArray();

                // Skills
                var skills = playerIndices[i].Select(ia => truth.Skills[players[ia]].Point).ToArray();

                // Sample peformances to get scores
                var scores = skills.Select(ia => new Gaussian(ia, performanceVariance).Sample()).ToArray();

                var game = new MultiPlayerGame { Id = (startIndex + i + 1).ToString("D") };

                for (int j = 0; j < playerIndices[i].Length; j++)
                {
                    // game.Teams.Add(new Team { Players = new[] { players[randomIndices[j]] }, Score = (int)scores[j] });
                    game.Players.Add(players[randomIndices[j]]);
                    game.Scores.Add((int)scores[j]);
                }

                games.Add(game);
            }

            return games;
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
        public override Results Train(Game game, IList<string> players, Marginals priors)
        {
            var multiPlayerGame = game as MultiPlayerGame;
            if (multiPlayerGame == null)
            {
                throw new InvalidOperationException("Incorrect game type");
            }

            this.numberOfPlayers.ObservedValue = players.Count;
            this.skillPriors.ObservedValue = players.Select(ia => priors.Skills[ia]).ToArray();
            this.drawMarginPrior.ObservedValue = priors.DrawMargin;

            this.playersPerGame.ObservedValue = multiPlayerGame.Players.Count;

            // order the indices of the players by descending order of score
            this.playerIndices.ObservedValue = multiPlayerGame.PlayersInDescendingScoreOrder.Select(players.IndexOf).ToArray();

            this.scores.ObservedValue = multiPlayerGame.Scores.OrderByDescending(x => x).ToArray();

            return new Results
                       {
                           Posteriors =
                               new Posteriors(
                               players,
                               this.engine.Infer<Gaussian[]>(this.skills),
                               this.engine.Infer<Gaussian>(this.drawMargin))
                       };
        }

        /// <summary>
        /// Predicts the outcome of a game.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="posteriors">The posteriors.</param>
        /// <returns>
        /// The <see cref="Prediction" />.
        /// </returns>
        public override Prediction PredictOutcome(Game game, Marginals posteriors)
        {
            // Not supported for this class
            return null;
        }
    }
}
