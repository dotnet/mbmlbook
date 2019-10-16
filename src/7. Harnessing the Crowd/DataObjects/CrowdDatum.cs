// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace HarnessingTheCrowd
{
    /// <summary>
    /// Class defining a single data point for crowd data
    /// </summary>
    public class CrowdDatum
    {
        /// <summary>
        /// Gets or sets the worker id.
        /// </summary>
        public string WorkerId { get; set; }

        /// <summary>
        /// Gets or sets the tweet id.
        /// </summary>
        public string TweetId { get; set; }

        /// <summary>
        /// Gets or sets the worker's label.
        /// </summary>
        public int WorkerLabel { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{TweetId}/{WorkerId}/{WorkerLabel}";
        }
    }
}