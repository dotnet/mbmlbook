// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Items
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using Microsoft.Research.Glo.ObjectModel;

    /// <summary>
    /// The match outcome (between players)
    /// </summary>
    public enum MatchOutcome
    {
        /// <summary>
        /// Player 1 wins.
        /// </summary>
        Player1Win = 0,

        /// <summary>
        /// Draw (can be determined by threshold).
        /// </summary>
        Draw = 1,

        /// <summary>
        /// Player 2 wins.
        /// </summary>
        Player2Win = 2,
    }

    /// <summary>
    /// The team match outcome
    /// </summary>
    public enum TeamMatchOutcome
    {
        /// <summary>
        /// Team 1 wins.
        /// </summary>
        Team1Win = 0,

        /// <summary>
        /// Draw (can be determined by threshold).
        /// </summary>
        Draw = 1,

        /// <summary>
        /// Team 2 wins.
        /// </summary>
        Team2Win = 2,
    }

    /// <summary>
    /// Game between n teams. For games between individuals, the team size is 1 for both teams
    /// </summary>
    public abstract class Game
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>
        /// The id.
        /// </value>
        public string Id { get; set; }

        /// <summary>
        /// Gets the players.
        /// </summary>
        [Browsable(false)]
        public abstract IList<string> Players { get; }

        /// <summary>
        /// Gets the scores.
        /// </summary>
        [Browsable(false)]
        public abstract IList<int> Scores { get; }

            /// <summary>
        /// Gets the player ids as a single string.
        /// </summary>
        [Browsable(false)]
        public string AllPlayers
        {
            get { return string.Join(", ", this.Players); }
        }

        /// <summary>
        /// Gets or sets the end time.
        /// </summary>
        /// <value>
        /// The end time.
        /// </value>
        [Browsable(false)]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the game type.
        /// </summary>
        [Browsable(false)]
        public string GameType { get; set; }

        /// <summary>
        /// Gets or sets the variant.
        /// </summary>
        public string Variant { get; set; }

        /// <summary>
        /// Gets the draw proportion.
        /// </summary>
        [Browsable(false)]
        public abstract double DrawProportion { get; }

    }

    /// <summary>
    /// The two player game.
    /// </summary>
    public class TwoPlayerGame : Game
    {
        /// <summary>
        /// Gets or sets the player 1.
        /// </summary>
        public string Player1 { get; set; }

        /// <summary>
        /// Gets or sets the player 2.
        /// </summary>
        public string Player2 { get; set; }

        /// <summary>
        /// Gets or sets the player 1 score.
        /// </summary>
        public int Player1Score { get; set; }

        /// <summary>
        /// Gets or sets the player 2 score.
        /// </summary>
        public int Player2Score { get; set; }

        /// <summary>
        /// Gets the players.
        /// </summary>
        public override IList<string> Players
        {
            get
            {
                return new[] { this.Player1, this.Player2 };
            }
        }

        /// <summary>
        /// Gets the scores.
        /// </summary>
        [Browsable(false)]
        public override IList<int> Scores
        {
            get
            {
                return new[] { this.Player1Score, this.Player2Score };
            }
        }

        /// <summary>
        /// Gets the outcome, based on the overall team score
        /// </summary>
        public MatchOutcome Outcome
        {
            get
            {
                if (this.Players.Count != 2)
                {
                    throw new InvalidOperationException("Should only be two players/teams");
                }

                return this.Player1Score == this.Player2Score
                           ? MatchOutcome.Draw
                           : (this.Player1Score > this.Player2Score ? MatchOutcome.Player1Win : MatchOutcome.Player2Win);
            }
        }

        /// <summary>
        /// Gets the draw proportion.
        /// </summary>
        [Browsable(false)]
        public override double DrawProportion
        {
            get
            {
                return this.Outcome == MatchOutcome.Draw ? 1.0 : 0.0;
            }
        }

        /// <summary>
        /// Creates the two player game.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="player1">The player1.</param>
        /// <param name="player2">The player2.</param>
        /// <param name="outcome">The outcome.</param>
        /// <returns>
        /// The <see cref="TwoPlayerGame" />.
        /// </returns>
        public static TwoPlayerGame CreateGame(string id, string player1, string player2, MatchOutcome outcome)
        {
            var game = new TwoPlayerGame { Id = id, Player1 = player1, Player2 = player2, Player1Score = 2 - (int)outcome, Player2Score = (int)outcome };
            ////var team1 = new Team { Score = 2 - (int)outcome };
            ////var team2 = new Team { Score = (int)outcome };
            ////team1.Players.Add(player1);
            ////team2.Players.Add(player2);
            return game;
        }
    }

    /// <summary>
    /// The multi player game.
    /// </summary>
    public class MultiPlayerGame : Game
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiPlayerGame"/> class.
        /// </summary>
        public MultiPlayerGame()
        {
            this.PlayerScores = new Dictionary<string, int>();
        }

        /// <summary>
        /// Gets or sets the player scores.
        /// </summary>
        [Browsable(false)]
        public Dictionary<string, int> PlayerScores { get; set; }

        /// <summary>
        /// Gets the players.
        /// </summary>
        [Browsable(true)]
        public override IList<string> Players => this.PlayerScores?.Keys.ToList();

        /// <summary>
        /// Gets or sets the scores.
        /// </summary>
        [Browsable(true)]
        public sealed override IList<int> Scores => this.PlayerScores?.Values.ToList();

        /// <summary>
        /// Gets the players in all of the teams in descending order of score.
        /// </summary>
        [Browsable(false)]
        public IList<string> PlayersInDescendingScoreOrder
        {
            get
            {
                return this.Players.Zip(this.Scores, (p, s) => new { p, s }).OrderByDescending(ia => ia.s).Select(ia => ia.p).ToList();
            }
        }

        /// <summary>
        /// Gets the outcomes between all possible pairings.
        /// </summary>
        [Browsable(false)]
        public MatchOutcome[][] Outcomes
        {
            get
            {
                var outcomes = new MatchOutcome[this.Scores.Count][];
                for (int i = 0; i < this.Scores.Count; i++)
                {
                    outcomes[i] = new MatchOutcome[this.Scores.Count];
                    for (int j = 0; j < this.Scores.Count; j++)
                    {
                        if (i == j)
                        {
                            outcomes[i][j] = MatchOutcome.Draw;
                            continue;
                        }

                        outcomes[i][j] = this.Scores[i] > this.Scores[j]
                                             ? MatchOutcome.Player1Win
                                             : this.Scores[j] > this.Scores[i] ? MatchOutcome.Player2Win : MatchOutcome.Draw;
                    }
                }

                return outcomes;
            }
        }

        /// <summary>
        /// Gets the draw proportion.
        /// </summary>
        [Browsable(false)]
        public override double DrawProportion
        {
            get
            {
                // Version 1: Just return draws between successive teams in ranking rather than all possible match-ups
                ////return 1.0 - (((double)this.Scores.GroupBy(x => x).Count() - 1) / (this.Scores.Length - 1));

                // Version 2: Return the average of all possible match-ups
                int n = this.Scores.Count;
                int possible = n * (n - 1) / 2;
                int cnt = 0;
                for (int i = 0; i < n; i++)
                {
                    for (int j = i + 1; j < n; j++)
                    {
                        if (this.Scores[i] == this.Scores[j])
                        {
                            cnt++;
                        }
                    }
                }

                return (double)cnt / possible;
            }
        }
    }

    /// <summary>
    /// The team game.
    /// </summary>
    public class TeamGame : Game
    {
        /// <summary>
        /// The player team indices.
        /// </summary>
        private Dictionary<string, int> playerTeamIndices;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamGame"/> class.
        /// </summary>
        public TeamGame()
        {
            this.Teams = new KeyedCollectionWithFunc<string, Team>(ia => ia.Id);
        }

        /// <summary>
        /// Gets the players.
        /// </summary>
        public override IList<string> Players
        {
            get
            {
                return this.Teams.SelectMany(ia => ia.PlayerScores.Keys).ToList();
            }
        }

        /// <summary>
        /// Gets or sets the scores.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">No setter available for this property</exception>
        public override IList<int> Scores
        {
            get
            {
                return this.Teams.Select(ia => ia.Score).ToList();
            }
        }

        /// <summary>
        /// Gets or sets the teams.
        /// </summary>
        /// <value>
        /// The teams.
        /// </value>
        [Browsable(false)]
        public KeyedCollectionWithFunc<string, Team> Teams { get; set; }

        /// <summary>
        /// Gets the team players.
        /// </summary>
        [Browsable(false)]
        public string TeamPlayers
        {
            get
            {
                return "{{" + string.Join("}, {", this.Teams.Select(ia => string.Join(", ", ia.PlayerScores.Keys))) + "}}";
            }
        }

        /// <summary>
        /// Gets the team scores.
        /// </summary>
        public IList<int> TeamScores
        {
            get
            {
                return this.Teams.Select(ia => ia.Score).ToArray();
            }
        }

        /// <summary>
        /// Gets the team counts.
        /// </summary>
        public IList<int> TeamCounts
        {
            get
            {
                return this.Teams.Select(ia => ia.PlayerScores.Count).ToArray();
            }
        }

            /// <summary>
        /// Gets the outcomes between all possible pairings.
        /// </summary>
        [Browsable(false)]
        public MatchOutcome[][] Outcomes
        {
            get
            {
                var outcomes = new MatchOutcome[this.Teams.Count][];
                for (int i = 0; i < this.Teams.Count; i++)
                {
                    outcomes[i] = new MatchOutcome[this.Teams.Count];
                    for (int j = 0; j < this.Teams.Count; j++)
                    {
                        if (i == j)
                        {
                            outcomes[i][j] = MatchOutcome.Draw;
                            continue;
                        }

                        outcomes[i][j] = this.Teams[i].Score > this.Teams[j].Score
                                             ? MatchOutcome.Player1Win
                                             : this.Teams[j].Score > this.Teams[i].Score ? MatchOutcome.Player2Win : MatchOutcome.Draw;
                    }
                }

                return outcomes;
            }
        }

        /// <summary>
        /// Gets the draw proportion.
        /// </summary>
        [Browsable(false)]
        public override double DrawProportion
        {
            get
            {
                // Version 1: Just return draws between successive teams in ranking rather than all possible match-ups
                ////return 1.0 - (((double)this.Scores.GroupBy(x => x).Count() - 1) / (this.Scores.Length - 1));

                // Version 2: Return the average of all possible match-ups
                int n = this.Teams.Count;
                int possible = n * (n - 1) / 2;
                int cnt = 0;
                for (int i = 0; i < n; i++)
                {
                    for (int j = i + 1; j < n; j++)
                    {
                        if (this.Teams[i].Score == this.Teams[j].Score)
                        {
                            cnt++;
                        }
                    }
                }

                return (double)cnt / possible;
            }
        }

        /// <summary>
        /// Gets the player team indices.
        /// </summary>
        [Browsable(false)]
        public Dictionary<string, int> PlayerTeamIndices
        {
            get
            {
                return this.playerTeamIndices ?? (this.playerTeamIndices = this.Players.ToDictionary(ia => ia, this.GetTeamIndex));
            }
        }
        
        /// <summary>
        /// Gets the outcome, based on the overall team score
        /// </summary>
        public TeamMatchOutcome Outcome
        {
            get
            {
                if (this.Teams.Count != 2)
                {
                    throw new InvalidOperationException("Should only be two teams");
                }

                return this.Teams[0].Score == this.Teams[1].Score
                           ? TeamMatchOutcome.Draw
                           : (this.Teams[0].Score > this.Teams[1].Score ? TeamMatchOutcome.Team1Win : TeamMatchOutcome.Team2Win);
            }
        }

        /// <summary>
        /// Gets the index of the team.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>The index.</returns>
        private int GetTeamIndex(string player)
        {
            for (int i = 0; i < this.Teams.Count; i++)
            {
                if (this.Teams[i].PlayerScores.ContainsKey(player))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
