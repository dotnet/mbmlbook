// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Experiments
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using MBMLViews;

    using Microsoft.ML.Probabilistic.Distributions;

    using MeetingYourMatch.Items;
    using MeetingYourMatch.Models;

#if NETFULL
    using Point = System.Windows.Point;
#else
    using Point = MBMLCommon.Point;
#endif

    /// <summary>
    /// The online experiment.
    /// </summary>
    [Serializable]
    public class OnlineExperiment : Experiment
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OnlineExperiment"/> class.
        /// </summary>
        public OnlineExperiment()
        {
            // Default constructor for serialization purposes only
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OnlineExperiment"/> class.
        /// </summary>
        /// <param name="modelFunc">The model function.</param>
        /// <param name="modelParameters">The model parameters.</param>
        public OnlineExperiment(Func<IModelParameters, IModel> modelFunc, IModelParameters modelParameters)
        {
            this.TrainModel = modelFunc(modelParameters);
            this.PredictModel = modelFunc(modelParameters);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OnlineExperiment"/> class.
        /// </summary>
        /// <param name="modelFunc">The model function.</param>
        /// <param name="modelParameters">The model parameters.</param>
        public OnlineExperiment(Func<IModelParameters, bool, bool, IModel> modelFunc, IModelParameters modelParameters)
        {
            this.TrainModel = modelFunc(modelParameters, false, true);
            this.PredictModel = modelFunc(modelParameters, false, false);
        }

        /// <summary>
        /// Gets or sets the skill prior. Required if new players are added later on.
        /// </summary>
        public Gaussian SkillPrior { get; set; }

        /// <summary>
        /// Gets or sets the draw margins.
        /// </summary>
        public List<Gaussian> DrawMargins { get; set; }

        /// <summary>
        /// Gets the draw margin means.
        /// </summary>
        public IList<double> DrawMarginMeans => DrawMargins?.Select(Utils.GetMean).ToArray();

        /// <summary>
        /// Gets the draw margin means and variances.
        /// </summary>
        public GaussianPoint[] DrawMarginMeansAndStandardDeviations => DrawMargins?.Select(Utils.GetMeanAndStandardDeviation).ToArray();

        /// <summary>
        /// Gets the latest posteriors.
        /// </summary>
        public Dictionary<string, Gaussian> LatestPosteriors
        {
            get
            {
                return PlayerPosteriors?.ToDictionary(ia => ia.Key, ia => ia.Value.Last());
            }
        }

        /// <summary>
        /// Gets the skill average.
        /// </summary>
        public double SkillAverage
        {
            get
            {
                return this.LatestPosteriors?.Average(ia => ia.Value.GetMean()) ?? 0;
            }
        }

        /// <summary>
        /// Gets the player trajectories.
        /// </summary>
        public Dictionary<string, Point[]> PlayerTrajectories
        {
            get
            {
                return PlayerPosteriors?.OrderByDescending(ia => ia.Value.Count)
                    .ThenByDescending(ia => Utils.GetMean(ia.Value.Last()))
                    .Take(10)
                    .OrderBy(ia => ia.Key)
                    .ToDictionary(ia => ia.Key, ia => ia.Value.Select((g, i) => new Point(i, Utils.GetMean(g))).ToArray());
            }
        }

        /// <summary>
        /// Gets the player trajectories with variances.
        /// </summary>
        public Dictionary<string, GaussianPoint[]> PlayerTrajectoriesWithStandardDeviations
        {
            get
            {
                return PlayerPosteriors?.OrderByDescending(ia => ia.Value.Count)
                    .ThenByDescending(ia => Utils.GetMean(ia.Value.Last()))
                    .Take(10)
                    .OrderBy(ia => ia.Key)
                    .ToDictionary(ia => ia.Key, ia => ia.Value.Select(Utils.GetMeanAndStandardDeviation).ToArray());
            }
        }

        /// <summary>
        /// Gets the top players.
        /// </summary>
        public Dictionary<string, GaussianPoint[]> TopTwoPlayerTrajectories
        {
            get
            {
                return PlayerPosteriors?.OrderByDescending(ia => Utils.ConservativeSkill(ia.Value.Last()))
                    .Take(2)
                    .OrderBy(ia => ia.Key)
                    .ToDictionary(ia => ia.Key, ia => ia.Value.Select(Utils.GetMeanAndStandardDeviation).ToArray());
            }
        }

        /// <summary>
        /// Gets or sets the predicted outcomes.
        /// </summary>
        public List<Prediction> Predictions { get; set; }

        /// <summary>
        /// Gets the cumulative errors.
        /// </summary>
        public int[] CumulativeErrors
        {
            get
            {
                if (Predictions == null || Predictions.Count == 0)
                {
                    return null;
                }

                var cumErrors = new int[this.Predictions.Count];
                cumErrors[0] = this.Predictions[0].Correct ? 0 : 1;
                for (int i = 1; i < this.Predictions.Count; i++)
                {
                    cumErrors[i] = cumErrors[i - 1] + (this.Predictions[i].Correct ? 0 : 1);
                }

                return cumErrors;
            }
        }

        /// <summary>
        /// Gets the cumulative error rate.
        /// </summary>
        public IEnumerable<double> CumulativeErrorRate
        {
            get
            {
                return CumulativeErrors?.Select((ia, i) => (double)ia / (i + 1));
            }
        }

        /// <summary>
        /// Gets the cumulative negative log probability of truth.
        /// </summary>
        public double?[] CumulativeNegativeLogProbOfTruth
        {
            get
            {
                if (this.Predictions == null || this.Predictions.Count == 0)
                {
                    return null;
                }

                var cumProb = new double?[this.Predictions.Count];
                cumProb[0] = -this.Predictions[0].LogProbOfTruth;
                if (double.IsNaN(cumProb[0].Value) || double.IsInfinity(cumProb[0].Value))
                    cumProb[0] = null;
                for (int i = 1; i < this.Predictions.Count; i++)
                {
                    if (cumProb[i - 1].HasValue)
                    {
                        cumProb[i] = (cumProb[i - 1] * i / (i + 1)) - (this.Predictions[i].LogProbOfTruth / (i + 1));

                        if (double.IsNaN(cumProb[i].Value) || double.IsInfinity(cumProb[i].Value))
                            cumProb[i] = null;
                    }
                    else
                        cumProb[i] = null;
                }

                return cumProb;
            }
        }

        /// <summary>
        /// Gets the first n players.
        /// </summary>
        /// <param name="experiment">The experiment.</param>
        /// <param name="count">The count.</param>
        /// <returns>The first N players from last experiment.</returns>
        public static IList<string> GetFirstNPlayers(OnlineExperiment experiment, int count)
        {
            return experiment.Players.Take(count).ToArray();
        }

        /// <summary>
        /// Gets the top n players by skill.
        /// </summary>
        /// <param name="experiment">The experiment.</param>
        /// <param name="count">The count.</param>
        /// <returns>
        /// The top N players from last experiment.
        /// </returns>
        public static IList<string> GetTopNPlayersBySkill(OnlineExperiment experiment, int count)
        {
            // Get top N players - leaderboard is already sorted
            return count == int.MaxValue ? experiment.LeaderBoard.Keys.ToArray() : experiment.LeaderBoard.Keys.Take(count).ToArray();
        }

        /// <summary>
        /// Gets the top n players by count.
        /// </summary>
        /// <param name="experiment">The experiment.</param>
        /// <param name="count">The count.</param>
        /// <returns>The top N players from last experiment.</returns>
        public static IList<string> GetTopNPlayersByCount(OnlineExperiment experiment, int count)
        {
            return experiment.PlayerPosteriors.OrderByDescending(ia => ia.Value.Count).Take(count).Select(ia => ia.Key).ToArray();
        }

        /// <summary>
        /// Gets the random n players.
        /// </summary>
        /// <param name="experiment">The experiment.</param>
        /// <param name="count">The count.</param>
        /// <returns>
        /// The top N players from last experiment.
        /// </returns>
        public static IList<string> GetRandomNPlayers(OnlineExperiment experiment, int count)
        {
            // Get top N players - leaderboard is already sorted
            return experiment.Players.OrderBy(ia => Guid.NewGuid()).Take(count).ToArray();
        }

        /// <summary>
        /// Gets the top players who have played at least 100 games.
        /// </summary>
        /// <param name="experiment">The experiment.</param>
        /// <param name="count">The count.</param>
        /// <returns>
        /// The top N players from last experiment.
        /// </returns>
        public static IList<string> GetTopPlayersWhoHavePlayedAtLeast100Games(
            OnlineExperiment experiment,
            int count)
        {
            var playersOver100 = experiment.PlayerPosteriors.Where(ia => ia.Value.Count >= 100).ToArray();
            return !playersOver100.Any() ? null : playersOver100.OrderByDescending(ia => ia.Value.Last().GetMean()).Take(count).Select(ia => ia.Key).ToArray();
        }

        /// <summary>
        /// Runs the specified model.
        /// </summary>
        /// <param name="games">The games.</param>
        /// <param name="count">The count.</param>
        /// <param name="verbose">if set to <c>true</c> [verbose].</param>
        public override void Run(IEnumerable<Game> games, int count, bool verbose)
        {
            this.PlayerPosteriors = this.Priors.Skills.ToDictionary(ia => ia.Key, ia => new List<Gaussian> { ia.Value });
            this.DrawMargins = new List<Gaussian> { this.Priors.DrawMargin };
            this.Predictions = new List<Prediction>();

            using (new CodeTimer(this.Name))
            {
                foreach (var game in games)
                {
                    var priors = new Marginals { DrawMargin = this.DrawMargins.Last() };

                    foreach (var player in game.Players)
                    {
                        if (!this.PlayerPosteriors.ContainsKey(player))
                        {
                            this.PlayerPosteriors[player] = new List<Gaussian> { this.SkillPrior };
                        }

                        priors.Skills[player] = this.PlayerPosteriors[player].Last();
                    }

                    // Predict outcome of game
                    var prediction = PredictModel?.PredictOutcome(game, priors);
                    if (prediction != null)
                    {
                        this.Predictions.Add(prediction);
                    }

                    // Train model using this game
                    this.LastResults = this.TrainModel.Train(game, game.Players, priors);

                    foreach (var player in game.Players)
                    {
                        this.PlayerPosteriors[player].Add(this.LastResults.Posteriors.Skills[player]);
                    }

                    this.DrawMargins.Add(this.LastResults.Posteriors.DrawMargin);

                    if (verbose)
                    {
                        var post = this.LastResults.Posteriors.Skills.Select(ia => $"{ia.Key}: {ia.Value}").ToArray();
                        Console.WriteLine(@"Game {0}, Posteriors: {1}", game.Id, string.Join(", ", post));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the latest posteriors.
        /// </summary>
        /// <param name="players">The players.</param>
        /// <returns>
        /// The latest posteriors.
        /// </returns>
        public Dictionary<string, Gaussian> GetLatestPosteriors(IEnumerable<string> players)
        {
            return players.ToDictionary(ia => ia, ia => this.PlayerPosteriors[ia].Last());
        }

        /// <summary>
        /// Gets the player posteriors.
        /// </summary>
        /// <param name="players">The players.</param>
        /// <returns>
        /// The posteriors.
        /// </returns>
        public Dictionary<string, List<Gaussian>> GetPlayerPosteriors(IEnumerable<string> players)
        {
            return players.ToDictionary(ia => ia, ia => this.PlayerPosteriors[ia]);
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