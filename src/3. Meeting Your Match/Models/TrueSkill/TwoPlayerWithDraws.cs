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
    /// The two player with draws.
    /// </summary>
    public class TwoPlayerWithDraws : ModelBase
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
        private readonly Variable<int> outcome;

        /// <summary>
        /// The draw margin.
        /// </summary>
        private readonly Variable<double> drawMargin;

        /// <summary>
        /// The draw margin prior.
        /// </summary>
        private readonly Variable<Gaussian> drawMarginPrior; 
        #endregion

        /// <summary>
        /// The engine.
        /// </summary>
        private readonly InferenceEngine engine;

        /// <summary>
        /// Initializes a new instance of the <see cref="TwoPlayerWithDraws" /> class.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="showFactorGraph">if set to <c>true</c> [show factor graph].</param>
        public TwoPlayerWithDraws(IModelParameters parameters, bool showFactorGraph = false) : base(parameters)
        {

            var performanceVariance = Variable.Observed(this.Parameters.PerformanceVariance).Named("performanceVariance");
            
                  
            this.drawMargin = Variable.New<double>().Named("drawMargin");
            this.drawMarginPrior = Variable.New<Gaussian>().Named("drawMarginPrior");
            this.drawMargin.SetTo(Variable<double>.Random(this.drawMarginPrior));
            Variable.ConstrainTrue(this.drawMargin > 0);


            this.skill1Prior = Variable.New<Gaussian>().Named("JSkillPrior").Attrib(new DoNotInfer());
            this.skill2Prior = Variable.New<Gaussian>().Named("FSkillPrior").Attrib(new DoNotInfer());

            this.player1skill = Variable.New<double>().Named("JSkill");
            this.player2skill = Variable.New<double>().Named("FSkill");

            player1skill.SetTo(Variable.Random<double, Gaussian>(skill1Prior));
            player2skill.SetTo(Variable.Random<double, Gaussian>(skill2Prior));

            var player1Performance = Variable.GaussianFromMeanAndVariance(player1skill, performanceVariance).Named("JPerf");
            var player2Performance = Variable.GaussianFromMeanAndVariance(player2skill, performanceVariance).Named("FPerf");
            var diff = (player1Performance - player2Performance).Named("diff");

            this.outcome = Variable.DiscreteUniform(3).Named("outcome");

            // The WinLoseDraw factor implemented using a constraint that's why it looks different in the factor graph viewer comparing to the book.    
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
        /// Samples from the model.
        /// </summary>
        /// <param name="truth">The truth.</param>
        /// <param name="players">The players.</param>
        /// <param name="performanceVariance">The performance variance.</param>
        /// <param name="drawMargin">The draw margin.</param>
        /// <returns>
        /// The <see cref="Game" />.
        /// </returns>
        public static Game Sample(Marginals truth, IList<string> players, double performanceVariance, double drawMargin)
        {
            double perf1 = new Gaussian(truth.Skills[players[0]].GetMean(), performanceVariance).Sample();
            double perf2 = new Gaussian(truth.Skills[players[1]].GetMean(), performanceVariance).Sample();

            double diff = perf1 - perf2;
            MatchOutcome outcome = diff < drawMargin ? MatchOutcome.Draw : (diff > 0 ? MatchOutcome.Player1Win : MatchOutcome.Player2Win);

            return TwoPlayerGame.CreateGame(Guid.NewGuid().ToString(), players[0], players[1], outcome);
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
            var twoPlayer = game as TwoPlayerGame;
            if (twoPlayer == null)
            {
                throw new InvalidOperationException("Multi-player/team games not supported");
            }

            var skills = players.Select(ia => priors.Skills[ia]).ToArray();
            this.skill1Prior.ObservedValue = skills[0];
            this.skill2Prior.ObservedValue = skills[1];
            this.outcome.ObservedValue = (int)twoPlayer.Outcome;

            this.drawMarginPrior.ObservedValue = priors.DrawMargin;

            return new Results
                       {
                           Posteriors =
                               new Posteriors(
                                   players,
                                   new Gaussian[] {
                                       this.engine.Infer<Gaussian>(this.player1skill),
                                       this.engine.Infer<Gaussian>(this.player2skill)
                                   },
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
        /// <exception cref="System.InvalidOperationException">Multi-player/team games not supported</exception>
        public override Prediction PredictOutcome(Game game, Marginals posteriors)
        {
            var twoPlayer = game as TwoPlayerGame;
            if (twoPlayer == null)
            {
                return null;
            }

            var skills = game.Players.Select(p => posteriors.Skills[p]).ToArray();
            this.skill1Prior.ObservedValue = skills[0];
            this.skill2Prior.ObservedValue = skills[1];
            this.drawMarginPrior.ObservedValue = posteriors.DrawMargin;

            var outcomePosterior = this.engine.Infer<Discrete>(this.outcome);

            // check if multimodal
            if (outcomePosterior.GetMode() == 0 && 
                Math.Abs(outcomePosterior.Evaluate(0) - outcomePosterior.Evaluate(2)) < double.Epsilon)
            {
                // Random outcome
                var randomOutcome = Rand.Int(2) == 0 ? MatchOutcome.Player1Win : MatchOutcome.Player2Win;
                return new TwoPlayerPrediction { Actual = twoPlayer.Outcome, Predicted = randomOutcome, IncludeDraws = true };
            }

            return new TwoPlayerPrediction
            {
                Actual = twoPlayer.Outcome,
                Predicted = (MatchOutcome)outcomePosterior.GetMode(),
                LogProbOfTruth = outcomePosterior.GetLogProb((int)twoPlayer.Outcome),
                IncludeDraws = true
            };
        }
    }
}
