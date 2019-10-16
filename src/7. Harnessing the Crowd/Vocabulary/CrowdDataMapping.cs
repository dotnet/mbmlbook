// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace HarnessingTheCrowd
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ML.Probabilistic.Math;

    /// <summary>
    /// The crowd data mapping. This class manages the mapping between the data
    /// (which is in the form of tweet, worker ids, and labels) and the model data
    /// (which is in term of indices).
    /// </summary>
    public class CrowdDataMapping
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CrowdDataMapping" /> class.
        /// </summary>
        /// <param name="data">
        /// The crowd data.
        /// </param>
        /// <param name="labelValueToString">
        /// The mapping from label values in file to label strings.
        /// </param>
        public CrowdDataMapping(
            CrowdData data,
            Dictionary<int, string> labelValueToString)
        {
            this.WorkerIndexToId = data.WorkerIds.ToArray();
            this.WorkerIdToIndex = this.WorkerIndexToId.Select((id, idx) => new KeyValuePair<string, int>(id, idx))
                .ToDictionary(x => x.Key, y => y.Value);
            this.TweetIds = data.TweetIds.ToArray();
            this.TweetIdToIndex = this.TweetIds.Select((id, idx) => new KeyValuePair<string, int>(id, idx))
                .ToDictionary(x => x.Key, y => y.Value);
            var labelsInFile = data.CrowdLabels.Select(d => d.WorkerLabel).Distinct().ToArray();

            var labelValueSet = new HashSet<int>(labelValueToString.Keys);
            if (!labelsInFile.All(lab => labelValueSet.Contains(lab)))
            {
                throw new ApplicationException("Unexpected labels found");
            }

            this.LabelValueToString = labelValueToString;
            this.LabelIndexToValue = labelValueSet.OrderBy(lab => lab).ToArray();
            this.LabelValueToIndex = this.LabelIndexToValue.Select((id, idx) => new KeyValuePair<int, int>(id, idx))
                .ToDictionary(x => x.Key, y => y.Value);

            this.Data = data;
            this.DataWithGold = new CrowdData
                {
                    CrowdLabels =
                        data.CrowdLabels.Where(d => data.GoldLabels.ContainsKey(d.TweetId))
                            .ToList(),
                    GoldLabels = data.GoldLabels
                };


            // WorkerLabelAccuracy: Perc. agreement between worker label and gold label
            var labelSet = this.DataWithGold.CrowdLabels;
            var goldLabels = this.DataWithGold.GoldLabels;
            var numLabels = labelSet.Count();
            var sumAcc = labelSet.Sum(datum => (datum.WorkerLabel == goldLabels[datum.TweetId] ? 1 : 0));

            this.AverageWorkerLabelAccuracy = sumAcc / (double)numLabels;
        }

        /// <summary>
        /// Gets the list of data.
        /// </summary>
        public CrowdData Data { get; }

        /// <summary>
        /// Gets the filtered enumerable list of data with raw gold labels.
        /// </summary>
        public CrowdData DataWithGold { get; }

        /// <summary>
        /// The number of label values.
        /// </summary>
        public int LabelCount => this.LabelValueToString.Count;

        /// <summary>
        /// Gets the mapping from the label value in the file to the label index in the model.
        /// </summary>
        public Dictionary<int, int> LabelValueToIndex { get; internal set; }

        /// <summary>
        /// Gets the mapping from the label index in the model to the label value.
        /// </summary>
        public int[] LabelIndexToValue { get; internal set; }

        /// <summary>
        /// Gets the mapping from the label index in the model to the label string.
        /// </summary>
        public string[] LabelIndexToString
        {
            get
            {
                return this.LabelIndexToValue.Select(val => this.LabelValueToString[val]).ToArray();
            }
        }

        /// <summary>
        /// Gets the mapping from the label value to the label string.
        /// </summary>
        public Dictionary<int, string> LabelValueToString { get; internal set; }

        /// <summary>
        /// The number of tweets.
        /// </summary>
        public int TweetCount => this.TweetIds.Length;

        /// <summary>
        /// Gets the mapping from the tweet id to the tweet index.
        /// </summary>
        public Dictionary<string, int> TweetIdToIndex { get; internal set; }

        /// <summary>
        /// Gets the tweet ids by index.
        /// </summary>
        public string[] TweetIds { get; internal set; }

        /// <summary>
        /// Gets the average accuracy of the worker labels.
        /// </summary>
        public double AverageWorkerLabelAccuracy { get; internal set; }

        /// <summary>
        /// The number of workers.
        /// </summary>
        public int WorkerCount => this.WorkerIndexToId.Length;

        /// <summary>
        /// Gets the mapping from the worker id to the worker index.
        /// </summary>
        public Dictionary<string, int> WorkerIdToIndex { get; }

        /// <summary>
        /// Gets the mapping from the worker index to the worker id.
        /// </summary>
        public string[] WorkerIndexToId { get; }

        /// <summary>
        /// Returns the matrix of model-ready labels (columns) of each worker (rows).
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The matrix of the labels (columns) of each worker (rows).</returns>
        public int[][] GetLabelsPerWorkerIndex(CrowdData data)
        {
            var result = new int[this.WorkerCount][];
            for (var i = 0; i < this.WorkerCount; i++)
            {
                var wid = this.WorkerIndexToId[i];
                result[i] = data.CrowdLabels.Where(d => d.WorkerId == wid).Select(d => this.LabelValueToIndex[d.WorkerLabel]).ToArray();
            }

            return result;
        }

        /// <summary>
        /// For each tweet Id, gets the random vote raw label.
        /// </summary>
        /// <param name="randomSeed">
        /// The random seed.
        /// </param>
        /// <returns>
        /// The dictionary of random vote labels indexed by tweet id.
        /// </returns>
        public Dictionary<string, int> GetRandomLabelsPerTweetId(int randomSeed = 12347)
        {
            var randomVotesPerTweetId = new Dictionary<string, int>();
            var numLabelValues = this.LabelCount;
            foreach (var d in this.Data.CrowdLabels)
            {
                 randomVotesPerTweetId[d.TweetId] = this.LabelIndexToValue[Rand.Int(numLabelValues)];
            }

            return randomVotesPerTweetId;
        }

        /// <summary>
        /// Returns the matrix of the tweet indices (columns) of each worker (rows).
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The matrix of the tweet indices (columns) of each worker (rows).</returns>
        public int[][] GetTweetIndicesPerWorkerIndex(CrowdData data)
        {
            var result = new int[this.WorkerCount][];
            for (var i = 0; i < this.WorkerCount; i++)
            {
                var wid = this.WorkerIndexToId[i];
                result[i] = data.CrowdLabels.Where(d => d.WorkerId == wid).Select(d => this.TweetIdToIndex[d.TweetId]).ToArray();
            }

            return result;
        }

        /// <summary>
        /// Restricts the mapping to a single tweet.
        /// </summary>
        /// <param name="tweetId">
        /// The tweet id.
        /// </param>
        /// <returns>
        /// The <see cref="CrowdDataMapping"/>.
        /// </returns>
        public virtual CrowdDataMapping RestrictToSingleTweet(string tweetId)
        {
            var data = this.Data.RestrictDataToSingleTweet(tweetId);
            return new CrowdDataMapping(data, this.LabelValueToString);
        }
    }
}