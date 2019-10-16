// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace HarnessingTheCrowd
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.ML.Probabilistic.Math;
    using Microsoft.ML.Probabilistic.Utilities;

    /// <summary>
    /// The crowd data.
    /// </summary>
    public class CrowdData
    {
        /// <summary>
        /// The mode - training or validation.
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// Training mode.
            /// </summary>
            Training,

            /// <summary>
            /// Validation mode.
            /// </summary>
            Validation
        }

        /// <summary>
        /// Gets the gold labels, indexed by tweet Id.
        /// </summary>
        public Dictionary<string, int> GoldLabels { get; internal set; }

        /// <summary>
        /// Gets the crowd labels.
        /// </summary>
        public List<CrowdDatum> CrowdLabels { get; internal set; }

        /// <summary>
        /// Gets the tweet ids.
        /// </summary>
        public IReadOnlyList<string> TweetIds
        {
            get
            {
                var result = this.GoldTweetIds;
                if (this.CrowdLabels != null)
                {
                    result = result.Concat(this.CrowdLabels.Select(cd => cd.TweetId)).Distinct().ToList();
                }

                return result;
            }
        }

        /// <summary>
        /// The number of tweets.
        /// </summary>
        public int NumTweets => this.TweetIds.Count;

        /// <summary>
        /// Gets the tweet ids with a gold label.
        /// </summary>
        public IReadOnlyList<string> GoldTweetIds =>
            this.GoldLabels != null ? this.GoldLabels.Keys.ToList() : new List<string>();

        /// <summary>
        /// The number of tweets with gold labels.
        /// </summary>
        public int NumGoldTweets => this.GoldTweetIds.Count;

        /// <summary>
        /// The number of labels.
        /// </summary>
        public int NumLabels => this.CrowdLabels?.Count ?? 0;

        /// <summary>
        /// Gets the worker ids.
        /// </summary>
        public IReadOnlyList<string> WorkerIds
        {
            get
            {
                return this.CrowdLabels != null
                           ? this.CrowdLabels.Select(cd => cd.WorkerId).Distinct().ToList()
                           : new List<string>();
            }
        }

        /// <summary>
        /// The number of workers.
        /// </summary>
        public int NumWorkers => this.WorkerIds.Count;

        /// <summary>
        /// Loads the data.
        /// </summary>
        /// <param name="crowdLabelsFileName">
        /// The crowd labels file name, format: (tweet id, worker id, worker label).
        /// </param>
        /// <param name="goldLabelsFileName">
        /// The gold labels file name, format:  (tweet id, gold label).
        /// </param>
        /// <param name="allowedLabels">
        /// The allowed Labels.
        /// </param>
        /// <returns>
        /// The crowd data.
        /// </returns>
        public static CrowdData LoadData(
            string crowdLabelsFileName,
            string goldLabelsFileName,
            HashSet<int> allowedLabels)
        {
            var crowdLabels = File.ReadLines(crowdLabelsFileName).Select(
                line =>
                    {
                        var strarr = line.Split('\t');
                        return new CrowdDatum
                                   {
                                       TweetId = strarr[0],
                                       WorkerId = strarr[1],
                                       WorkerLabel = int.Parse(strarr[2]),
                                   };
                    }).Where(cd => allowedLabels.Contains(cd.WorkerLabel)).ToList();

            var goldLabels = File.ReadLines(goldLabelsFileName).Select(line => line.Split('\t'))
                .Where(arr => int.TryParse(arr[1], out var lab) && allowedLabels.Contains(lab))
                .ToDictionary(arr => arr[0], arr => int.Parse(arr[1]));

            return new CrowdData { CrowdLabels = crowdLabels, GoldLabels = goldLabels };
        }

        /// <summary>
        /// Gets the majority vote labels, indexed by tweet Id.
        /// </summary>
        /// <param name="crowdLabels">
        /// The crowd labels.
        /// </param>
        /// <returns>
        /// The majority label keyed by tweet id.
        /// </returns>
        public static Dictionary<string, int> MajorityVoteLabels(List<CrowdDatum> crowdLabels)
        {
            return crowdLabels?.GroupBy(d => d.TweetId).ToDictionary(
                t => t.Key,
                t => t.GroupBy(d => d.WorkerLabel)
                    .Select(g => new { label = g.Key, count = g.Count() })).ToDictionary(
                kvp => kvp.Key,
                kvp =>
                    {
                        var max = kvp.Value.Max(a => a.count);
                        var majorityLabs = kvp.Value.Where(a => a.count == max).Select(a => a.label).ToArray();
                        var index = (majorityLabs.Length > 1)
                                        ? 0
                                        : Rand.Int(majorityLabs.Length);
                        return majorityLabs[index];
                    });
        }

        /// <summary>
        /// Split data. The validation data just consists of the specified fraction of tweets that contain gold labels,
        /// along with all crowd labels for those tweets.
        /// The training data consists of the labels (including gold labels), of all other tweets.
        /// </summary>
        /// <param name="fractionGoldLabelsForTraining">
        /// The fraction gold label tweets for training.
        /// </param>
        /// <param name="randomSeed">
        /// The random seed.
        /// </param>
        /// <returns>
        /// The crowd data for the split.
        /// </returns>
        public Dictionary<Mode, CrowdData> SplitData(
            double fractionGoldLabelsForTraining,
            int randomSeed)
        {
            Rand.Restart(randomSeed);
            var allTweets = this.CrowdLabels.GroupBy(cd => cd.TweetId)
                .ToDictionary(grp => grp.Key, grp => grp.ToList());
            var allTweetIds = new HashSet<string>(allTweets.Keys);

            // The tweet ids in some order
            var goldTweetIds = this.GoldLabels.Keys.ToArray();
            var goldPerm = Rand.Perm(goldTweetIds.Length);
            var numGoldForTraining = (int)(fractionGoldLabelsForTraining * goldTweetIds.Length);
            var goldPermForTraining = goldPerm.Take(numGoldForTraining).ToList();
            var goldPermForValidation = goldPerm.Skip(numGoldForTraining).ToList();

            var goldTweetIdsForValidation =
                goldPermForValidation.Select(idx => goldTweetIds[idx]).ToList();

            var allTweetIdsForTraining = allTweetIds.Except(goldTweetIdsForValidation).ToArray();

            var trainingData = allTweetIdsForTraining.SelectMany(id => allTweets[id]).ToList();
            var validationData = goldTweetIdsForValidation.SelectMany(id => allTweets[id]).ToList();

            var trainingGoldLabels = goldPermForTraining.ToDictionary(
                idx => goldTweetIds[idx],
                idx => this.GoldLabels[goldTweetIds[idx]]);
            var validationGoldLabels = goldPermForValidation.ToDictionary(
                idx => goldTweetIds[idx],
                idx => this.GoldLabels[goldTweetIds[idx]]);
            var result = new Dictionary<Mode, CrowdData>
                             {
                                 [Mode.Training] =
                                     new CrowdData
                                         {
                                             CrowdLabels = trainingData.ToList(),
                                             GoldLabels = trainingGoldLabels
                                         },
                                 [Mode.Validation] =
                                     new CrowdData
                                         {
                                             CrowdLabels =
                                                 validationData.ToList(),
                                             GoldLabels = validationGoldLabels
                                         }
                             };

            return result;
        }

        /// <summary>
        /// Limits the data in various ways
        /// </summary>
        /// <param name="maxJudgements">
        /// Maximum number of judgments.
        /// </param>
        /// <param name="maxNumTweets">
        /// The maximum number of tweets. If less than the full number, these are chosen randomly
        /// except that tweets with gold labels are chosen preferentially.
        /// </param>
        /// <param name="maxNumWorkers">
        /// The maximum number of workers. If less than the full number, these are chosen
        /// to maximize the number of labels
        /// </param>
        /// <param name="maxJudgmentsPerWorker">
        /// The maximum number of judgments per worker.
        /// </param>
        /// <param name="maxJudgmentsPerTweet">
        /// The maximum number of judgments per tweet.
        /// </param>
        /// <param name="balanceTweetsByLabel">Balance tweets by majority vote label.
        /// </param>
        /// <param name="randomSeed">
        /// The random seed.
        /// </param>
        /// <returns>
        /// The reduced crowd data.
        /// </returns>
        public virtual CrowdData LimitData(
            int maxJudgements = int.MaxValue,
            int maxNumTweets = int.MaxValue,
            int maxNumWorkers = int.MaxValue,
            int maxJudgmentsPerWorker = int.MaxValue,
            int maxJudgmentsPerTweet = int.MaxValue,
            bool balanceTweetsByLabel = false,
            int randomSeed = 12347)
        {
            var crowdLabels = this.CrowdLabels;
            var goldLabels = this.GoldLabels;

            // Restrict the tweets if requested.
            Rand.Restart(randomSeed);
            var selectedTweets = new HashSet<string>(crowdLabels.Select(cd => cd.TweetId).Distinct());
            if (selectedTweets.Count > maxNumTweets || balanceTweetsByLabel)
            {
                var goldTweets = goldLabels.Keys.ToArray();
                var nonGoldTweets = selectedTweets.Except(goldTweets).ToArray();

                var goldPerm = Rand.Perm(goldTweets.Length);
                var nonGoldPerm = Rand.Perm(nonGoldTweets.Length);

                var permutedGoldTweets = goldPerm.Select(i => goldTweets[i]);
                var permutedNonGoldTweets = nonGoldPerm.Select(i => nonGoldTweets[i]);
                var selectedTweetList = permutedGoldTweets.Concat(permutedNonGoldTweets).Take(maxNumTweets).ToList();
                selectedTweets = new HashSet<string>(selectedTweetList);
                crowdLabels = crowdLabels.Where(cd => selectedTweets.Contains(cd.TweetId)).ToList();

                if (balanceTweetsByLabel)
                {
                    var majorityVoteLabels = CrowdData.MajorityVoteLabels(crowdLabels);
                    var tweetCountsPerLabel = majorityVoteLabels.Values.GroupBy(lab => lab).Select(grp => grp.Count()).ToArray();
                    var smallestCount = tweetCountsPerLabel.Min();
                    selectedTweets.Clear();

                    var counts = Util.ArrayInit(tweetCountsPerLabel.Length, i => 0);

                    Util.ArrayInit(tweetCountsPerLabel.Length, labVal => new List<int>());
                    foreach (var tweet in selectedTweetList)
                    {
                        var lab = majorityVoteLabels[tweet];
                        if (++counts[lab] <= smallestCount)
                        {
                            selectedTweets.Add(tweet);
                        }
                    }

                    crowdLabels = crowdLabels.Where(cd => selectedTweets.Contains(cd.TweetId)).ToList();
                }

                goldLabels = goldLabels.Where(kvp => selectedTweets.Contains(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            // Limit workers
            var numWorkers = crowdLabels.Select(cd => cd.WorkerId).Distinct().Count();
            if (numWorkers > maxNumWorkers)
            {
                crowdLabels = crowdLabels.GroupBy(cd => cd.WorkerId)
                    .OrderByDescending(wcds => wcds.Count(wcd => selectedTweets.Contains(wcd.TweetId)))
                    .Take(maxNumWorkers).SelectMany(cds => cds).ToList();
            }

            // Limit judgments per worker
            if (maxJudgmentsPerWorker < int.MaxValue)
            {
                crowdLabels = crowdLabels.GroupBy(cd => cd.WorkerId).Select(
                    gp =>
                        {
                            var judgments = gp.ToArray();
                            var perm = Rand.Perm(gp.Count());
                            var limitCount = Math.Min(perm.Length, maxJudgmentsPerWorker);
                            var limitedJudgments = Util.ArrayInit(limitCount, i => judgments[perm[i]]);
                            return limitedJudgments;
                        }).SelectMany(cds => cds).ToList();
            }

            // Limit judgments per tweet
            if (maxJudgmentsPerTweet < int.MaxValue)
            {
                crowdLabels = crowdLabels.GroupBy(cd => cd.TweetId).Select(
                    gp =>
                        {
                            var judgments = gp.ToArray();
                            var perm = Rand.Perm(gp.Count());
                            var limitCount = Math.Min(perm.Length, maxJudgmentsPerTweet);
                            var limitedJudgments = Util.ArrayInit(limitCount, i => judgments[perm[i]]);
                            return limitedJudgments;
                        }).SelectMany(cds => cds).ToList();
            }

            // Limit the total judgments
            if (maxJudgements < int.MaxValue)
            {
                var numJudgments = crowdLabels.Count;
                var perm = Rand.Perm(numJudgments);
                var limitedNumJudgments = Math.Min(numJudgments, maxJudgements);
                crowdLabels = Util.ArrayInit(limitedNumJudgments, i => crowdLabels[perm[i]]).ToList();
            }

            return new CrowdData { CrowdLabels = crowdLabels, GoldLabels = goldLabels };
        }

        /// <summary>
        /// Restrict the data to a single tweet.
        /// </summary>
        /// <param name="tweetId">The tweet id.</param>
        /// <returns>The restricted data.</returns>
        public virtual CrowdData RestrictDataToSingleTweet(
            string tweetId)
        {
            var crowdLabels = this.CrowdLabels.Where(cd => cd.TweetId == tweetId).ToList();
            var goldLabels = this.GoldLabels?.Where(g => g.Key == tweetId).ToDictionary(g => g.Key, g => g.Value);

            return new CrowdData { CrowdLabels = crowdLabels, GoldLabels = goldLabels };
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"#T:{this.NumTweets}, #G:{this.NumGoldTweets}, #W: {this.NumWorkers}, #L:{this.NumLabels}";
        }

        public class WorkerTweetEqualityComparer : IEqualityComparer<CrowdDatum>
        {
            public static WorkerTweetEqualityComparer Instance { get; } = new WorkerTweetEqualityComparer();

            public bool Equals(CrowdDatum x, CrowdDatum y)
            {
                return x?.WorkerId == y?.WorkerId && x?.TweetId == y?.TweetId;
            }

            public int GetHashCode(CrowdDatum cd)
            {
                return Hash.Combine(cd.WorkerId.GetHashCode(), cd.TweetId.GetHashCode());
            }
        }
    }
}
