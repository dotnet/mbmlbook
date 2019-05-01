// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Items
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The team.
    /// </summary>
    [Serializable]
    public class Team
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Team"/> class.
        /// </summary>
        public Team()
        {
            ////this.Players = new List<string>();
            this.PlayerScores = new Dictionary<string, int>();
        }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>
        /// The id.
        /// </value>
        public string Id { get; set; }

        /////// <summary>
        /////// Gets or sets the players.
        /////// </summary>
        ////public IList<string> Players { get; set; }

        /////// <summary>
        /////// Gets or sets the score.
        /////// </summary>
        /////// <value>
        /////// The score.
        /////// </value>
        ////public int Score { get; set; }

        /// <summary>
        /// Gets or sets the player scores.
        /// </summary>
        public Dictionary<string, int> PlayerScores { get; set; }

        /// <summary>
        /// Gets the score.
        /// </summary>
        public int Score
        {
            get
            {
                return this.PlayerScores.Sum(ia => ia.Value);
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0}: {1}", this.Id, this.Score);
        }
    }
}
