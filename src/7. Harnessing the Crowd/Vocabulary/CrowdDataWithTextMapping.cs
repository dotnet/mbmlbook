// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace HarnessingTheCrowd
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// This class manages the mapping between the data (which is
    /// in the form of tweet ids, worker ids, and labels) and the model data (which is in term of indices).
    /// </summary>
    public class CrowdDataWithTextMapping : CrowdDataMapping
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:CrowdSourcing.WordsDataMapping" /> class.
        /// </summary>
        /// <param name="data">
        /// The data.
        /// </param>
        /// <param name="labelValueToString">
        /// The mapping from label values in file to label strings.
        /// </param>
        /// <param name="corpusInfo">
        /// The corpus information.
        /// </param>
        /// <param name="maxWordsPerTweet">The maximum number of words considered per tweet.</param>
        public CrowdDataWithTextMapping(
            CrowdDataWithText data,
            Dictionary<int, string> labelValueToString,
            CorpusInformation corpusInfo)
            : base(data, labelValueToString)
        {
            this.CorpusInfo = corpusInfo;
            var docs = this.TweetIds.Select(tid => data.TweetTexts[tid]).ToArray();

            this.WordIndicesPerTweetIndex = docs.Select(
                doc =>
                    {
                        var indices = corpusInfo.GetWordIndices(doc).ToArray();
                        return indices;
                    }).ToArray();
            this.WordCountsPerTweetIndex = this.WordIndicesPerTweetIndex.Select(arr => arr.Length).ToArray();
        }

        /// <summary>
        /// Gets the vocabulary
        /// </summary>
        public CorpusInformation CorpusInfo { get; internal set; }

        /// <summary>
        /// Gets the size of the vocabulary.
        /// </summary>
        public int WordCount => this.CorpusInfo.NumberOfWords;

        /// <summary>
        /// Gets the word counts per tweet index
        /// </summary>
        public int[] WordCountsPerTweetIndex { get; internal set; }

        /// <summary>
        /// Gets the word indices per tweet index
        /// </summary>
        public int[][] WordIndicesPerTweetIndex { get; internal set; }

        /// <inheritdoc />
        public override CrowdDataMapping RestrictToSingleTweet(string tweetId)
        {
            var crowdMapping = base.RestrictToSingleTweet(tweetId);
            return new CrowdDataWithTextMapping((CrowdDataWithText)crowdMapping.Data, this.LabelValueToString, this.CorpusInfo);
        }
    }
}
