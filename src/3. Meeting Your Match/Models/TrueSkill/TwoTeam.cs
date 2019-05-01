// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Models.TrueSkill
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Math;
    using Microsoft.ML.Probabilistic.Models;

    using global::MeetingYourMatch.Experiments;

    using global::MeetingYourMatch.Items;
    using Microsoft.ML.Probabilistic.Models.Attributes;

    /// <summary>
    /// The multi team.
    /// </summary>
    public class TwoTeam : ModelBase
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
        /// The outcome.
        /// </summary>
        private readonly Variable<int> outcome;

        /// <summary>
        /// The team 1 count.
        /// </summary>
        private readonly Variable<int> team1Count;

        /// <summary>
        /// The team 2 count.
        /// </summary>
        private readonly Variable<int> team2Count;

        /// <summary>
        /// The team 1 players.
        /// </summary>
        private readonly VariableArray<int> team1Players;

        /// <summary>
        /// The team 2 players.
        /// </summary>
        private readonly VariableArray<int> team2Players;
        
        #endregion

        /// <summary>
        /// The engine.
        /// </summary>
        private readonly InferenceEngine engine;

        /// <summary>
        /// Initializes a new instance of the <see cref="TwoTeam" /> class.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="showFactorGraph">if set to <c>true</c> [show factor graph].</param>
        public TwoTeam(IModelParameters parameters, bool showFactorGraph = false) : base(parameters)
        {
            this.numberOfPlayers = Variable.New<int>().Named("numberOfPlayers").Attrib(new DoNotInfer());
            var dynamicsVariance = Variable.Observed(this.Parameters.DynamicsVariance).Named("dynamicsVariance");
            var performanceVariance = Variable.Observed(this.Parameters.PerformanceVariance).Named("performanceVariance");

            Range player = new Range(this.numberOfPlayers).Named("player");

            this.team1Count = Variable.New<int>().Named("team1Count").Attrib(new DoNotInfer());
            Range team1Player = new Range(this.team1Count).Named("team1Player");

            this.team2Count = Variable.New<int>().Named("team2Count").Attrib(new DoNotInfer());
            Range team2Player = new Range(this.team2Count).Named("team2Player");

            this.skillPriors = Variable.Array<Gaussian>(player).Named("skillPriors").Attrib(new DoNotInfer());
            this.skills = Variable.Array<double>(player).Named("skills");
            this.skills[player] = Variable.GaussianFromMeanAndVariance(Variable<double>.Random(this.skillPriors[player]), dynamicsVariance);

            this.drawMargin = Variable.New<double>().Named("drawMargin");
            this.drawMarginPrior = Variable.New<Gaussian>().Named("drawMarginPrior");
            this.drawMargin.SetTo(Variable<double>.Random(this.drawMarginPrior));
            Variable.ConstrainTrue(this.drawMargin > 0);

            this.team1Players = Variable.Array<int>(team1Player).Named("team1Players").Attrib(new DoNotInfer());
            this.team2Players = Variable.Array<int>(team2Player).Named("team2Players").Attrib(new DoNotInfer());
            
            var team1Skills = Variable.Subarray(this.skills, this.team1Players).Named("team1Skills");
            var team2Skills = Variable.Subarray(this.skills, this.team2Players).Named("team2Skills");
            
            var team1Performances = Variable.Array<double>(team1Player).Named("team1Performances");
            team1Performances[team1Player] = Variable.GaussianFromMeanAndVariance(team1Skills[team1Player], performanceVariance);
            var team1Performance = Variable.Sum(team1Performances).Named("team1Performance");
            
            var team2Performances = Variable.Array<double>(team2Player).Named("team2Performances");
            team2Performances[team2Player] = Variable.GaussianFromMeanAndVariance(team2Skills[team2Player], performanceVariance);
            var team2Performance = Variable.Sum(team2Performances).Named("team2Performance");

            this.outcome = Variable.DiscreteUniform(3).Named("outcome");


            var diff = (team1Performance - team2Performance).Named("diff");
                
            using (Variable.Case(this.outcome, 0))
            {
                // player 1 wins
                Variable.ConstrainTrue(diff > this.drawMargin);
            }

            using (Variable.Case(this.outcome, 1))
            {
                // draw
                Variable.ConstrainBetween(diff, -this.drawMargin, this.drawMargin);
            }

            using (Variable.Case(this.outcome, 2))
            {
                // player 2 wins
                Variable.ConstrainTrue(diff < -this.drawMargin);
            }
            
            this.engine = Utils.GetDefaultEngine(showFactorGraph);
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
        /// <exception cref="System.NotSupportedException">Multi-team games not supported</exception>
        public override Results Train(Game game, IList<string> players, Marginals priors)
        {
            var teamGame = game as TeamGame;
            if (teamGame == null)
            {
                throw new NotSupportedException("Multi-team games not supported");
            }

            this.numberOfPlayers.ObservedValue = teamGame.Players.Count;
            this.skillPriors.ObservedValue = teamGame.Players.Select(ia => priors.Skills[ia]).ToArray();
            this.drawMarginPrior.ObservedValue = priors.DrawMargin;

            this.team1Count.ObservedValue = teamGame.TeamCounts[0];
            this.team2Count.ObservedValue = teamGame.TeamCounts[1];

            this.team1Players.ObservedValue = teamGame.Teams[0].PlayerScores.Keys.Select(players.IndexOf).ToArray();
            this.team2Players.ObservedValue = teamGame.Teams[1].PlayerScores.Keys.Select(players.IndexOf).ToArray();
            
            this.outcome.ObservedValue = (int)teamGame.Outcome;

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
            var teamGame = game as TeamGame;
            if (teamGame == null)
            {
                // unsupported game type
                return null;
            }

            this.numberOfPlayers.ObservedValue = teamGame.Players.Count;
            this.skillPriors.ObservedValue = teamGame.Players.Select(ia => posteriors.Skills[ia]).ToArray();
            this.drawMarginPrior.ObservedValue = posteriors.DrawMargin;

            this.team1Count.ObservedValue = teamGame.TeamCounts[0];
            this.team2Count.ObservedValue = teamGame.TeamCounts[1];

            this.team1Players.ObservedValue = teamGame.Teams[0].PlayerScores.Keys.Select(teamGame.Players.IndexOf).ToArray();
            this.team2Players.ObservedValue = teamGame.Teams[1].PlayerScores.Keys.Select(teamGame.Players.IndexOf).ToArray();

            var outcomePosterior = this.engine.Infer<Discrete>(this.outcome);
            double logProbOfTruth = outcomePosterior.GetLogProb((int)teamGame.Outcome);
            
            // check if multimodal
            if (outcomePosterior.GetMode() == 0 &&
                Math.Abs(outcomePosterior.Evaluate(0) - outcomePosterior.Evaluate(2)) < double.Epsilon)
            {
                // Random outcome
                var randomOutcome = Rand.Int(2) == 0 ? TeamMatchOutcome.Team1Win : TeamMatchOutcome.Team2Win;
                return new TwoTeamPrediction
                           {
                               Actual = teamGame.Outcome,
                               Predicted = randomOutcome,
                               IncludeDraws = true,
                               LogProbOfTruth = logProbOfTruth
                           };
            }

            return new TwoTeamPrediction
            {
                Actual = teamGame.Outcome,
                Predicted = (TeamMatchOutcome)outcomePosterior.GetMode(),
                LogProbOfTruth = logProbOfTruth,
                IncludeDraws = true
            };
        }
    }
}
