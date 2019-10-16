// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace HarnessingTheCrowd
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Contains all data corresponding to a particular tweet
    /// </summary>
    public class Tweet
    {
        /// <summary>
        /// Gets or sets the tweet Id
        /// </summary>
        public string TweetId { get; set; }

        /// <summary>
        /// Gets or sets the tweet text.
        /// </summary>
        public string TweetText { get; set; }

        /// <summary>
        /// Gets or sets the optional gold label.
        /// </summary>
        public int? GoldLabel { get; set; }

        /// <summary>
        /// Gets or sets the worker labels
        /// </summary>
        public Dictionary<string, int> WorkerLabels { get; set; }

        /// <summary>
        /// Creates tweet info from crowd data.
        /// </summary>
        /// <param name="crowdData">
        /// The crowd data.
        /// </param>
        /// <returns>
        /// The tweet information.
        /// </returns>
        public static Dictionary<string, Tweet> FromCrowdData(CrowdDataWithText crowdData)
        {
            var tweets = crowdData.TweetTexts;
            return crowdData.CrowdLabels.GroupBy(cd => cd.TweetId).ToDictionary(
                grp => grp.Key,
                grp =>
                    {
                        return new Tweet
                                   {
                                       TweetId = grp.Key,
                                       TweetText =
                                           tweets == null || !tweets.ContainsKey(grp.Key)
                                               ? string.Empty
                                               : tweets[grp.Key],
                                       GoldLabel =
                                           crowdData.GoldLabels == null
                                           || !crowdData.GoldLabels.ContainsKey(grp.Key)
                                               ? (int?)null
                                               : crowdData.GoldLabels[grp.Key],

                                       // Occasionally a worker labels the same tweet more than once, so just take the first.
                                       WorkerLabels =
                                           grp.GroupBy(cd => cd.WorkerId).Select(grp1 => grp1.First())
                                               .ToDictionary(cd => cd.WorkerId, cd => cd.WorkerLabel)
                                   };
                    });
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var result = $"{this.TweetText ?? this.TweetId} (# worker labels: {this.WorkerLabels.Count})";
            if (this.GoldLabel != null)
            {
                result += $" (Gold label: {this.GoldLabel})";
            }

            return result;
        }
    }
}