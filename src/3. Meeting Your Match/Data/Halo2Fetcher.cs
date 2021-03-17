// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Halo2Fetcher.cs" company="Microsoft">
//   Copyright (C) Microsoft. All rights reserved.
// </copyright>
// <summary>
//   The Halo2 fetcher.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using MBMLViews;
using MeetingYourMatch.Items;
using Microsoft.Research.Glo;
using Microsoft.Research.Glo.ObjectModel;

namespace MeetingYourMatch.Data
{
    /// <summary>
    /// The Halo 2 fetcher.
    /// </summary>
    public class Halo2Fetcher
    {
        /// <summary>
        /// Load halo 2 data by game type.
        /// </summary>
        /// <typeparam name="TGame">The type of the game.</typeparam>
        /// <param name="filepath">The path to file to import.</param>
        public static Inputs<TGame> LoadByGameType<TGame>(string filepath)
            where TGame : Game, new()
        {
            // Values from TrueSkill paper
            const double Mu = 25.0;
            const double Sigma = Mu / 3;
            const double Beta = Sigma / 2;
            const double Gamma = Sigma / 100;


            var games = LoadFromCsv<TGame>(filepath);

            var inputs = new Inputs<TGame> { Mu = Mu, Sigma = Sigma, Beta = Beta, Gamma = Gamma, Games = games };

            RemoveBogusGames(inputs);

            return inputs;
        }

        /// <summary>
        /// Loads Halo game from CSV.
        /// <br />
        /// Each line of the 4 files contains the following fields:
        /// Date and Time when the game finished (PST time zone)
        /// Unique game ID (valid across all 4 game modes)
        /// Variant of the game (3 different values)
        /// 1 = Capture the Flag
        /// 2 = Slayer
        /// 9 = Assault
        /// Map ID on which the game was played (5 different values)
        /// Unique player ID (valid across all 4 game modes)
        /// Team association
        /// Score
        /// <br />
        /// Note that each game is split over two rows
        /// </summary>
        /// <typeparam name="TGame">The type of the game.</typeparam>
        /// <param name="filename">The filename.</param>
        /// <exception cref="System.IO.IOException">Too many team ids for this game id</exception>
        private static KeyedCollectionWithFunc<string, TGame> LoadFromCsv<TGame>(string filename)
            where TGame : Game, new()
        {

            var games = new KeyedCollectionWithFunc<string, TGame>(game => game.Id);
            var columns = (List<string[]>)SerializationManager.Load(filename);
            var idMapping = new Dictionary<string, string>();

            foreach (var row in columns)
            {
                if (row == null || row.Length != 7)
                {
                    continue;
                }

                var gameId = row[1];
                var teamId = "Team" + row[5];
                var playerId = row[4];
                var endTime = DateTime.Parse(row[0]);
                var variant = int.Parse(row[2]);
                var score = int.Parse(row[6]);

                if (!idMapping.ContainsKey(playerId))
                {
                    idMapping[playerId] = $"Gamer{idMapping.Count:00000}";
                }

                // Create new game if it doesn't exist yet
                TGame game;
                if (games.Contains(gameId))
                {
                    game = games[gameId];
                }
                else
                {
                    game = new TGame { Id = gameId, EndTime = endTime, GameType = "Halo2", Variant = ((HaloVariant)variant).ToString() };
                    games.Add(game);
                }

                switch (game)
                {
                    case TeamGame teamGame:
                    {
                        Team team;
                        if (teamGame.Teams.Contains(teamId))
                        {
                            team = teamGame.Teams[teamId];
                        }
                        else
                        {
                            team = new Team {Id = teamId};
                            teamGame.Teams.Add(team);
                        }

                        team.PlayerScores[idMapping[playerId]] = score;
                        continue;
                    }

                    case TwoPlayerGame twoPlayerGame:
                    {
                        if (twoPlayerGame.Player1 == null)
                        {
                            twoPlayerGame.Player1 = idMapping[playerId];
                            twoPlayerGame.Player1Score = score;
                        }
                        else
                        {
                            twoPlayerGame.Player2 = idMapping[playerId];
                            twoPlayerGame.Player2Score = score;
                        }

                        continue;
                    }

                    case MultiPlayerGame multiPlayerGame:
                    {
                        multiPlayerGame.PlayerScores[idMapping[playerId]] = score;
                        continue;
                    }

                    default:
                        throw new InvalidOperationException("Unknown game type");
                }
            }

            return games;
        }

        /// <summary>
        /// Removes bogus games.
        /// </summary>
        /// <typeparam name="TGame">The type of the game.</typeparam>
        /// <param name="inputs">The inputs.</param>
        private static void RemoveBogusGames<TGame>(Inputs<TGame> inputs)
            where TGame : Game
        {
            Console.WriteLine(@"Removing bogus games");

            int numGames = inputs.Games.Count;

            // Remove games with only 1 player
            // Remove games where all scores are zero
            inputs.Games.RemoveAll(ia => ia.Players.Count < 2 || ia.Scores.All(x => x == 0));

            Console.WriteLine(@"Before {0}, Removed {1}, After {2}", numGames, numGames - inputs.Games.Count, inputs.Games.Count);
        }
    }
}
