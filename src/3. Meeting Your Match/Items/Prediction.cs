// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Items
{
    /// <summary>
    /// The prediction.
    /// </summary>
    public abstract class Prediction
    {
        /// <summary>
        /// Gets or sets the log prob of truth.
        /// </summary>
        public abstract double LogProbOfTruth { get; set; }

        /// <summary>
        /// Gets a value indicating whether correct.
        /// </summary>
        public abstract bool Correct { get; }
    }
}
