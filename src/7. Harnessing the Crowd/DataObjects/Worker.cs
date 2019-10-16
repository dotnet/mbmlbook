// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace HarnessingTheCrowd
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// All the data relating to a worker worker.
    /// </summary>
    public class Worker
    {
        /// <summary>
        /// Gets or sets the worker Id
        /// </summary>
        public string WorkerId { get; set; }

        /// <summary>
        /// Gets or sets the tweets that this worker judged.
        /// </summary>
        public List<Tweet> JudgedTweets { get; set; }

        /// <summary>
        /// Creates worker info from a crowd datum.
        /// </summary>
        /// <param name="crowdData">
        /// The crowd data.
        /// </param>
        /// <returns>
        /// The <see cref="Worker"/>.
        /// </returns>
        public static Dictionary<string, Worker> FromCrowdData(CrowdDataWithText crowdData)
        {
            return crowdData.CrowdLabels.GroupBy(cd => cd.WorkerId).ToDictionary(
                grp => grp.Key,
                grp => new Worker
                           {
                               WorkerId = grp.Key,
                               JudgedTweets = grp.Select(cd => crowdData.Tweets?[cd.TweetId]).ToList()
                           });
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Worker {this.WorkerId} judged {this.JudgedTweets.Count} tweets";
        }
    }
}
