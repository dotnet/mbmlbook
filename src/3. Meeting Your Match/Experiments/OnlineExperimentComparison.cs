// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Experiments
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ML.Probabilistic.Collections;
    using Microsoft.ML.Probabilistic.Distributions;

    using MeetingYourMatch.Items;

    /// <summary>
    /// The online experiment collection.
    /// </summary>
    /// <typeparam name="TGame">The type of the game.</typeparam>
    [Serializable]
    public class OnlineExperimentComparison<TGame> : ExperimentComparison<OnlineExperiment, TGame>
        where TGame : Game
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OnlineExperimentComparison{TGame}"/> class.
        /// </summary>
        public OnlineExperimentComparison()
        {
            // Default constructor for serialization purposes only
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OnlineExperimentComparison{TGame}"/> class.
        /// </summary>
        /// <param name="experiments">
        /// The experiments.
        /// </param>
        public OnlineExperimentComparison(params OnlineExperiment[] experiments) : this()
        {
            if (experiments != null)
            {
                this.Experiments.AddRange(experiments);
            }
        }

        /// <summary>
        /// Gets the player trajectories. Only show for small experiments.
        /// </summary>
        public Dictionary<string, double[]> PlayerTrajectories
        {
            get
            {
                return this.Experiments.Count == 0 || this.Experiments[0].PlayerCount > 5
                            ? null 
                            : this.GetTrajectories(OnlineExperiment.GetTopNPlayersBySkill, Utils.GetMean, int.MaxValue);
            }
        }

        /// <summary>
        /// Gets the player trajectories with truth.
        /// </summary>
        public Dictionary<string, double[]> PlayerTrajectoriesWithTruth
        {
            get
            {
                if (this.Truth == null || this.Truth.Count != this.Experiments.Count || this.Experiments.Count == 0
                    || this.Experiments[0].PlayerCount > 5)
                {
                    return null;
                }

                var trajectories = this.GetTrajectories(OnlineExperiment.GetTopNPlayersBySkill, Utils.GetMean, int.MaxValue);
                this.Truth.ForEach(ia => trajectories[ia.Key + " (Truth)"] = ia.Value.ToArray());
                return trajectories;
            }
        }

        /// <summary>
        /// Gets the player trajectories.Only show for small experiments.
        /// </summary>
        public Dictionary<string, GaussianPoint[]> PlayerTrajectoriesWithSigma
        {
            get
            {
                return this.Experiments.Count == 0 || this.Experiments[0].PlayerCount > 5
                            ? null
                            : this.GetTrajectories(OnlineExperiment.GetTopNPlayersBySkill, Utils.GetMeanAndStandardDeviation, int.MaxValue);
            }
        }

        /// <summary>
        /// Gets the player trajectories with sigma and truth.
        /// </summary>
        public Dictionary<string, GaussianPoint[]> PlayerTrajectoriesWithSigmaAndTruth
        {
            get
            {
                if (this.Truth == null || this.Experiments.Count == 0) 
                {
                    return null;
                }
                
                return this.GetTrajectories(OnlineExperiment.GetFirstNPlayers, Utils.GetMeanAndStandardDeviation, 1, true);
            }
        }

        /// <summary>
        /// Gets the top player trajectories.
        /// </summary>
        public Dictionary<string, double[]> TopPlayerTrajectories
        {
            get
            {
                return this.Experiments.Count == 0 
                            ? null
                            : this.GetTrajectories(OnlineExperiment.GetTopNPlayersBySkill, Utils.GetMean, 2);
            }
        }

        /// <summary>
        /// Gets the top player trajectories with bounds.
        /// </summary>
        public Dictionary<string, GaussianPoint[]> RandomPlayerTrajectoriesWithSigma
        {
            get
            {
                return this.Experiments.Count == 0
                           ? null
                           : this.GetTrajectories(OnlineExperiment.GetRandomNPlayers, Utils.GetMeanAndStandardDeviation, 2);
            }
        }

        /// <summary>
        /// Gets the convergence demo.
        /// </summary>
        public Dictionary<string, GaussianPoint[]> ConvergenceDemo
        {
            get
            {
                return this.Experiments.Count == 0
                           ? null
                           : this.GetTrajectories(
                               OnlineExperiment.GetTopPlayersWhoHavePlayedAtLeast100Games,
                               Utils.GetMeanAndStandardDeviation,
                               2);
            }
        }

        /// <summary>
        /// Gets the top player trajectories with bounds.
        /// </summary>
        public Dictionary<string, GaussianPoint[]> TopPlayerTrajectoriesWithSigma
        {
            get
            {
                return this.Experiments.Count == 0
                           ? null
                           : this.GetTrajectories(OnlineExperiment.GetTopNPlayersBySkill, Utils.GetMeanAndStandardDeviation, 2);
            }
        }

        /// <summary>
        /// Gets the top player trajectories with bounds.
        /// </summary>
        public Dictionary<string, GaussianPoint[]> MostPlayedTrajectoriesWithSigma
        {
            get
            {
                return this.Experiments.Count == 0
                           ? null
                           : this.GetTrajectories(OnlineExperiment.GetTopNPlayersByCount, Utils.GetMeanAndStandardDeviation, 2);
            }
        }

        /// <summary>
        /// Gets the top player trajectories with bounds.
        /// </summary>
        public Dictionary<string, GaussianPoint[]> TopPlayerTrajectoriesWithThreeSigma
        {
            get
            {
                return this.Experiments.Count == 0
                           ? null
                           : this.GetTrajectories(OnlineExperiment.GetTopNPlayersBySkill, Utils.GetMeanAndThreeSigma, 2);
            }
        }

        /// <summary>
        /// Gets the top two player final posteriors.
        /// </summary>
        public Dictionary<string, object> FinalPosteriors
        {
            get
            {
                if (this.Experiments.Count == 0)
                {
                    return null;
                }

                var players = OnlineExperiment.GetTopNPlayersBySkill(this.Experiments.Last(), 2);

                var dict = new Dictionary<string, object>
                {
                    ["Player"] = players
                };
                foreach (var experiment in this.Experiments)
                {
                    dict[experiment.Name] = experiment.GetLatestPosteriors(players);
                }

                return dict;
            }
        }

        /// <summary>
        /// Gets the cumulative errors.
        /// </summary>
        public Dictionary<string, double[]> CumulativeErrors
        {
            get
            {
                return this.Experiments.Count == 0
                           ? null
                           : this.Experiments.Where(ia => ia.CumulativeErrors != null).ToDictionary(
                               ia => ia.Name,
                               ia => ia.CumulativeErrors.Select(Convert.ToDouble).ToArray());
            }
        }

        /// <summary>
        /// Gets the cumulative error rates.
        /// </summary>
        public Dictionary<string, double[]> CumulativeErrorRates
        {
            get
            {
                return this.Experiments.Count == 0
                           ? null
                           : this.Experiments.Where(ia => ia.CumulativeErrorRate != null)
                                 .ToDictionary(ia => ia.Name, ia => ia.CumulativeErrorRate.ToArray());
            }
        }

        /// <summary>
        /// Gets the cumulative negative log probability of truth.
        /// </summary>
        public Dictionary<string, double?[]> CumulativeNegativeLogProbOfTruth
        {
            get
            {
                return this.Experiments.Count == 0
                           ? null
                           : this.Experiments.Where(ia => ia.CumulativeNegativeLogProbOfTruth != null && ia.Name != "Random")
                                 .ToDictionary(ia => ia.Name, ia => ia.CumulativeNegativeLogProbOfTruth);
            }
        }

        /// <summary>
        /// Gets the skill averages.
        /// </summary>
        public Dictionary<string, double> SkillAverages
        {
            get
            {
                return this.Experiments.Count == 0 ? null : this.Experiments.ToDictionary(ia => ia.Name, ia => ia.SkillAverage);
            }
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the trajectories.
        /// </summary>
        /// <typeparam name="T">The type of the means.</typeparam>
        /// <param name="playerFunc">The player function.</param>
        /// <param name="valueFunc">The value function.</param>
        /// <param name="count">The count.</param>
        /// <returns>
        /// The <see cref="Dictionary{TKey, TValue}" />.
        /// </returns>
        internal Dictionary<string, T[]> GetTrajectories<T>(Func<OnlineExperiment, int, IList<string>> playerFunc, Func<Gaussian, int, T> valueFunc, int count, bool includeTruth = false)
        {
            var exp = this.Experiments.First(ia => ia.Name != "Elo" && ia.Name != "Random");
            
            var players = playerFunc(exp, count);
            if (players == null)
            {
                return null;
            }

            var dict = new Dictionary<string, T[]>();

            foreach (var experiment in this.Experiments.Where(ia => ia.Name != "Random"))
            {
                var playerPosteriors = experiment.GetPlayerPosteriors(players);
                
                foreach (var kvp in playerPosteriors)
                {
                    dict[$"{kvp.Key} ({experiment.Name})"] = kvp.Value.Select(valueFunc).ToArray();
                    if (includeTruth && this.Truth != null)
                    {
                        dict[kvp.Key + " (Truth)"] = this.Truth[kvp.Key].Select(Utils.GetGaussianPoint).Cast<T>().ToArray();
                    }
                }
            }

            return dict;
        }
    }
}
