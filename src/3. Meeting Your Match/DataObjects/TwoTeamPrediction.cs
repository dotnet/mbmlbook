// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Items
{
    /// <summary>
    /// The two team prediction.
    /// </summary>
    public class TwoTeamPrediction : Prediction
    {
        /// <summary>
        /// Gets or sets the predicted outcome.
        /// </summary>
        public TeamMatchOutcome Predicted { get; set; }

        /// <summary>
        /// Gets or sets the actual outcome.
        /// </summary>
        public TeamMatchOutcome Actual { get; set; }

        /// <summary>
        /// Gets or sets the log probability of truth.
        /// </summary>
        public override double LogProbOfTruth { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include draws.
        /// </summary>
        public bool IncludeDraws { get; set; }

        /// <summary>
        /// Gets a value indicating whether correct.
        /// </summary>
        public override bool Correct
        {
            get
            {
                return this.IncludeDraws
                           ? this.Actual == this.Predicted
                           : (this.Actual == TeamMatchOutcome.Team1Win) == (this.Predicted == TeamMatchOutcome.Team1Win);
            }
        }
    }
}