// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Research.Glo.ObjectModel;

    using Microsoft.ML.Probabilistic.Collections;
    using Microsoft.ML.Probabilistic.Distributions;

    using MeetingYourMatch.Items;
    using MeetingYourMatch.Models;

    using TeamCollection = Microsoft.Research.Glo.ObjectModel.KeyedCollectionWithFunc<string, Items.Team>;

    /// <summary>
    /// The halo variant.
    /// </summary>
    public enum HaloVariant
    {
        /// <summary>
        /// The capture the flag.
        /// </summary>
        CaptureTheFlag = 1,

        /// <summary>
        /// The slayer.
        /// </summary>
        Slayer = 2,

        /// <summary>
        /// The assault.
        /// </summary>
        Assault = 9
    }

    /// <summary>
    /// The inputs. Contains Players and Games, as well as default Elo/TrueSkill parameters for the game type.
    /// </summary>
    /// <typeparam name="TGame">The type of the game.</typeparam>
    [Serializable]
    public class Inputs<TGame> where TGame : Game
    {
        /// <summary>
        /// The game matrix
        /// </summary>
        private Dictionary<string, Dictionary<string, int>> gameMatrix;

        /// <summary>
        /// The players.
        /// </summary>
        private HashSet<string> players;

        /// <summary>
        /// Initializes a new instance of the <see cref="Inputs{TGame}"/> class. 
        /// </summary>
        public Inputs()
        {
            this.Games = new KeyedCollectionWithFunc<string, TGame>(ia => ia.Id);
        }

        /// <summary>
        /// Gets the jill.
        /// </summary>
        public static string Jill => "Jill";

        /// <summary>
        /// Gets the fred.
        /// </summary>
        public static string Fred => "Fred";

        /// <summary>
        /// Gets or sets the initial mean.
        /// </summary>
        public double Mu { get; set; }

        /// <summary>
        /// Gets or sets the initial standard deviation.
        /// </summary>
        public double Sigma { get; set; }

        /// <summary>
        /// Gets the initial variance.
        /// </summary>
        public double InitialVariance => this.Sigma * this.Sigma;

        /// <summary>
        /// Gets or sets the performance standard deviation.
        /// </summary>
        public double Beta { get; set; }

        /// <summary>
        /// Gets the performance variance.
        /// </summary>
        public double PerformanceVariance => this.Beta * this.Beta;

        /// <summary>
        /// Gets or sets the dynamics standard deviation.
        /// </summary>
        public double Gamma { get; set; }

        /// <summary>
        /// Gets the dynamics variance.
        /// </summary>
        public double DynamicsVariance => this.Gamma * this.Gamma;

        /// <summary>
        /// Gets the skill prior.
        /// </summary>
        public Gaussian SkillPrior => new Gaussian(this.Mu, this.InitialVariance);

        /// <summary>
        /// Gets the true skill priors.
        /// </summary>
        public Priors TrueSkillPriors =>
            new Priors
            {
                DrawMargin = new Gaussian(1, 10),
                Skills = this.Players.ToDictionary(ia => ia, ia => this.SkillPrior)
            };

        /// <summary>
        /// Gets the draw proportion.
        /// </summary>
        public double DrawProportion
        {
            get
            {
                if (Games == null)
                    return 0;
                return Games.Any() ? Games.Average(ia => ia.DrawProportion) : 0;
            }
        }

        /// <summary>
        /// Gets or sets the games.
        /// </summary>
        /// <value>
        /// The games.
        /// </value>
        public KeyedCollectionWithFunc<string, TGame> Games { get; set; }

        /// <summary>
        /// Gets the players.
        /// </summary>
        public HashSet<string> Players => this.players ?? (this.players = new HashSet<string>(this.Games.SelectMany(ia => ia.Players)));

        /// <summary>
        /// Gets the number of players.
        /// </summary>
        /// <value>
        /// The number of players.
        /// </value>
        public int NumberOfPlayers => this.Players.Count;

        /// <summary>
        /// Gets the number of games.
        /// </summary>
        /// <value>
        /// The number of games.
        /// </value>
        public int NumberOfGames => this.Games.Count;

        /// <summary>
        /// Gets the true skill parameters.
        /// </summary>
        public TrueSkillParameters TrueSkillParameters =>
            new TrueSkillParameters
            {
                DynamicsVariance = this.DynamicsVariance,
                PerformanceVariance = this.PerformanceVariance
            };

        /// <summary>
        /// Gets the game matrix. This is a dictionary of dictionaries that contains number of games played against each opponent.
        /// Note that this is a sparse representation (entries only appear if at least one game was played). This is
        /// symmetric - the players for each game are ordered in the order in which they first appear in the dataset.
        /// </summary>
        public Dictionary<string, Dictionary<string, int>> GameMatrix
        {
            get
            {
                if (this.gameMatrix != null && this.gameMatrix.Count > 0)
                {
                    return this.gameMatrix;
                }

                this.gameMatrix = new Dictionary<string, Dictionary<string, int>>();

                // Find two players who have played the most
                foreach (var game in this.Games)
                {
                    var plyrs = game.Players.OrderBy(ia => ia).ToArray();

                    foreach (var player in plyrs)
                    {
                        if (!this.gameMatrix.ContainsKey(player))
                        {
                            this.gameMatrix[player] = new Dictionary<string, int>();
                        }

                        // Copy to local variable to avoid access to modified closure
                        var player1 = player;
                        foreach (var other in plyrs.Where(p => p != player1))
                        {
                            if (!this.gameMatrix[player].ContainsKey(other))
                            {
                                this.gameMatrix[player][other] = 1;
                            }
                            else
                            {
                                this.gameMatrix[player][other] += 1;
                            }
                        }
                    }
                }

                return this.gameMatrix;
            }
        }

        /// <summary>
        /// Gets the max played.
        /// </summary>
        public List<Tuple<string, string, int>> MaxPlayed
        {
            get
            {
                //// Look for players who play a lot together and those that don't
                var maxPlayed =
                    (from kvp in this.GameMatrix
                     let max = kvp.Value.ArgMax(ia => ia.Value)
                     select new Tuple<string, string, int>(kvp.Key, max.Key, max.Value)).ToList();
                return maxPlayed.OrderByDescending(ia => ia.Item3).ToList();
            }
        }

        /// <summary>
        /// Gets the game counts.
        /// </summary>
        public Dictionary<string, int> GameCounts
        {
            get
            {
                var gameCounts = new Dictionary<string, int>();
                foreach (var player in this.Games.SelectMany(game => game.Players))
                {
                    if (!gameCounts.ContainsKey(player))
                    {
                        gameCounts[player] = 1;
                    }
                    else
                    {
                        gameCounts[player] += 1;
                    }
                }

                return gameCounts.OrderByDescending(ia => ia.Value).ToDictionary(ia => ia.Key, ia => ia.Value);
            }
        }

        /// <summary>
        /// Creates the toy inputs.
        /// </summary>
        /// <param name="matchOutcomes">The match outcomes.</param>
        /// <returns>The <see cref="Inputs{TwoPlayerGame}"/>.</returns>
        public static Inputs<TwoPlayerGame> CreateToyInputs(IEnumerable<MatchOutcome> matchOutcomes)
        {
            var inputs = new Inputs<TwoPlayerGame>
            {
                Mu = 120,
                Sigma = 40,
                Beta = 5,
                Gamma = 1.2,
                Games = new KeyedCollectionWithFunc<string, TwoPlayerGame>(ia => ia.Id),
            };

            inputs.Games.AddRange(matchOutcomes.Select((ia, i) => TwoPlayerGame.CreateGame(i.ToString("N"), Jill, Fred, ia)));

            return inputs;
        }

        /// <summary>
        /// Creates the toy inputs.
        /// </summary>
        /// <param name="matchOutcomes">The match outcomes.</param>
        /// <returns>The <see cref="Inputs{MultiPlayerGame}"/>.</returns>
        public static Inputs<MultiPlayerGame> CreateToyInputs(IEnumerable<Dictionary<string, int>> matchOutcomes)
        {
            var inputs = new Inputs<MultiPlayerGame>
                             {
                                 Mu = 120,
                                 Sigma = 40,
                                 Beta = 5,
                                 Gamma = 1.2,
                                 Games = new KeyedCollectionWithFunc<string, MultiPlayerGame>(ia => ia.Id),
                             };

            inputs.Games.AddRange(matchOutcomes.Select((ia, i) => new MultiPlayerGame { Id = i.ToString("N"), PlayerScores = ia }));
            return inputs;
        }
    }
}
