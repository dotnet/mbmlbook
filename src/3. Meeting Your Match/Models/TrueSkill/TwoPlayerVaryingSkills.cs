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
    using Microsoft.ML.Probabilistic.Models.Attributes;

    /// <summary>
    /// The two player.
    /// </summary>
    public class TwoPlayerVaryingSkills : ModelBase
    {
        #region Variables
        /// <summary>
        /// The skill prior of the 1st player.
        /// </summary>
        private readonly Variable<Gaussian> skill1Prior;
        /// <summary>
        /// The skill prior of the 2nd player.
        /// </summary>
        private readonly Variable<Gaussian> skill2Prior;

        /// <summary>
        /// The skills.
        /// </summary>
        private readonly Variable<double> player1skill;
        private readonly Variable<double> player2skill;

        /// <summary>
        /// The outcome.
        /// </summary>
        private readonly Variable<bool> outcome;
        #endregion

        /// <summary>
        /// The engine.
        /// </summary>
        private readonly InferenceEngine engine;

        /// <summary>
        /// Initializes a new instance of the <see cref="TwoPlayerVaryingSkills" /> class.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="showFactorGraph">if set to <c>true</c> [show factor graph].</param>
        public TwoPlayerVaryingSkills(IModelParameters parameters, bool showFactorGraph = false) : base(parameters)
        {

            var dynamicsVariance = Variable.Observed(this.Parameters.DynamicsVariance).Named("dynamicsVariance");
            var performanceVariance = Variable.Observed(this.Parameters.PerformanceVariance).Named("performanceVariance");

            this.skill1Prior = Variable.New<Gaussian>().Named("skill1Prior").Attrib(new DoNotInfer());
            this.skill2Prior = Variable.New<Gaussian>().Named("skill2Prior").Attrib(new DoNotInfer());

            this.player1skill = Variable.GaussianFromMeanAndVariance(Variable<double>.Random(skill1Prior), dynamicsVariance).Named("player1skill");
            this.player2skill = Variable.GaussianFromMeanAndVariance(Variable<double>.Random(skill2Prior), dynamicsVariance).Named("player2skill");

            var player1Performance = Variable.GaussianFromMeanAndVariance(player1skill, performanceVariance).Named("player1Performance");
            var player2Performance = Variable.GaussianFromMeanAndVariance(player2skill, performanceVariance).Named("player2Performance");

            this.outcome = (player1Performance > player2Performance).Named("player1wins");
        
            this.engine = Utils.GetDefaultEngine(showFactorGraph);
        }

        /// <summary>
        /// Samples from the model.
        /// </summary>
        /// <param name="truth">The truth.</param>
        /// <param name="players">The players.</param>
        /// <param name="performanceVariance">The performance variance.</param>
        /// <returns>The <see cref="Game"/>.</returns>
        public static TwoPlayerGame Sample(Marginals truth, IList<string> players, double performanceVariance)
        {
            double perf1 = new Gaussian(truth.Skills[players[0]].GetMean(), performanceVariance).Sample();
            double perf2 = new Gaussian(truth.Skills[players[1]].GetMean(), performanceVariance).Sample();

            return TwoPlayerGame.CreateGame(
                Guid.NewGuid().ToString(),
                players[0],
                players[1],
                perf1 > perf2 ? MatchOutcome.Player1Win : MatchOutcome.Player2Win);
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
            var TwoPlayerVaryingSkills = game as TwoPlayerGame;
            if (TwoPlayerVaryingSkills == null)
            {
                throw new InvalidOperationException("Multi-player/team games not supported");
            }

            var skills = players.Select(ia => priors.Skills[ia]).ToArray();
            this.skill1Prior.ObservedValue = skills[0];
            this.skill2Prior.ObservedValue = skills[1];
            this.outcome.ObservedValue = TwoPlayerVaryingSkills.Outcome == MatchOutcome.Player1Win;

            return new Results { Posteriors = new Posteriors(players, new Gaussian[] { this.engine.Infer<Gaussian>(this.player1skill), this.engine.Infer<Gaussian>(this.player2skill) })};
        }

        /// <summary>
        /// Predicts the outcome (ranking) of a game.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="posteriors">The posteriors.</param>
        /// <returns>
        /// The <see cref="TwoPlayerVaryingSkillsPrediction" />.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">Multi-player/team games not supported</exception>
        public override Prediction PredictOutcome(Game game, Marginals posteriors)
        {
            var TwoPlayerVaryingSkills = game as TwoPlayerGame;
            if (TwoPlayerVaryingSkills == null)
            {
                return null;
            }

            var skills = game.Players.Select(p => posteriors.Skills[p]).ToArray();
            this.skill1Prior.ObservedValue = skills[0];
            this.skill2Prior.ObservedValue = skills[1];

            var outcomePosterior = this.engine.Infer<Bernoulli>(this.outcome);

            return new TwoPlayerPrediction
                       {
                           Actual = TwoPlayerVaryingSkills.Outcome,
                           Predicted =
                               outcomePosterior.GetProbTrue() > 0.5 ? MatchOutcome.Player1Win : MatchOutcome.Player2Win,
                           LogProbOfTruth =
                               TwoPlayerVaryingSkills.Outcome == MatchOutcome.Draw
                                   ? double.NaN
                                   : outcomePosterior.GetLogProb(TwoPlayerVaryingSkills.Outcome == MatchOutcome.Player1Win),
                           IncludeDraws = false
                       };
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
