// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace HarnessingTheCrowd
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// The crowd data with text.
    /// </summary>
    public class CrowdDataWithText : CrowdData
    {
        /// <summary>
        /// Gets the tweet texts, keyed by tweet id.
        /// </summary>
        public Dictionary<string, string> TweetTexts { get; internal set; }

        /// <summary>
        /// Gets the tweet information.
        /// </summary>
        public Dictionary<string, Tweet> Tweets { get; internal set; }

        /// <summary>
        /// Gets the worker information.
        /// </summary>
        public Dictionary<string, Worker> Workers { get; internal set; }

        /// <summary>
        /// Loads the data.
        /// </summary>
        /// <param name="crowdLabelsFileName">
        /// The crowd labels file name, format: (tweet id, worker id, worker label).
        /// </param>
        /// <param name="goldLabelsFileName">
        /// The gold labels file name, format:  (tweet id, gold label).
        /// </param>
        /// <param name="textsFileName">
        /// The texts File Name.
        /// </param>
        /// <param name="allowedLabels">
        /// The allowed labels.
        /// </param>
        /// <returns>
        /// The crowd data.
        /// </returns>
        public static CrowdDataWithText LoadData(
            string crowdLabelsFileName,
            string goldLabelsFileName,
            string textsFileName,
            HashSet<int> allowedLabels)
        {
            var crowdData = CrowdData.LoadData(crowdLabelsFileName, goldLabelsFileName, allowedLabels);

            var texts = File.ReadLines(textsFileName).Select(line => line.Split('\t'))
                .ToDictionary(strarr => strarr[0], strarr => strarr[1]);

            var tweetSet = new HashSet<string>(crowdData.CrowdLabels.Select(cd => cd.TweetId).Distinct());
            var tweetsForData = texts.Where(kvp => tweetSet.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var result = new CrowdDataWithText { CrowdLabels = crowdData.CrowdLabels, GoldLabels = crowdData.GoldLabels, TweetTexts = tweetsForData };
            result.Tweets = Tweet.FromCrowdData(result);
            result.Workers = Worker.FromCrowdData(result);
            return result;
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
        public new Dictionary<Mode, CrowdDataWithText> SplitData(double fractionGoldLabelsForTraining, int randomSeed = 12347)
        {
            var baseResult = base.SplitData(fractionGoldLabelsForTraining, randomSeed);

            var trainingCrowdData = baseResult[Mode.Training];
            var trainingIds = new HashSet<string>(trainingCrowdData.TweetIds);
            var validationCrowdData = baseResult[Mode.Validation];
            var validationIds = new HashSet<string>(validationCrowdData.TweetIds);
            var trainingResult = new CrowdDataWithText
                                     {
                                         CrowdLabels = trainingCrowdData.CrowdLabels,
                                         GoldLabels = trainingCrowdData.GoldLabels,
                                         TweetTexts =
                                             this.TweetTexts
                                                 .Where(kvp => trainingIds.Contains(kvp.Key))
                                                 .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                                     };

            trainingResult.Tweets = Tweet.FromCrowdData(trainingResult);
            trainingResult.Workers = Worker.FromCrowdData(trainingResult);

            var validationResult = new CrowdDataWithText
                                       {
                                           CrowdLabels = validationCrowdData.CrowdLabels,
                                           GoldLabels = validationCrowdData.GoldLabels,
                                           TweetTexts =
                                               this.TweetTexts
                                                   .Where(kvp => validationIds.Contains(kvp.Key))
                                                   .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                                       };

            validationResult.Tweets = Tweet.FromCrowdData(validationResult);
            validationResult.Workers = Worker.FromCrowdData(validationResult);

            var result = new Dictionary<Mode, CrowdDataWithText>
                             {
                                 [Mode.Training] = trainingResult,
                                 [Mode.Validation] = validationResult
                             };

            return result;
        }

        /// <inheritdoc />
        public override CrowdData LimitData(
            int maxJudgments = int.MaxValue,
            int maxNumTweets = int.MaxValue,
            int maxNumWorkers = int.MaxValue,
            int maxJudgmentsPerWorker = int.MaxValue,
            int maxJudgmentsPerTweet = int.MaxValue,
            bool balanceTweetsByLabel = false,
            int randomSeed = 12347)
        {
            var baseResult = base.LimitData(maxJudgments, maxNumTweets, maxNumWorkers, maxJudgmentsPerWorker, maxJudgmentsPerTweet, balanceTweetsByLabel, randomSeed);
            var tweetIds = new HashSet<string>(baseResult.TweetIds);
            var result = new CrowdDataWithText
                       {
                           CrowdLabels = baseResult.CrowdLabels,
                           GoldLabels = baseResult.GoldLabels,
                           TweetTexts =
                               this.TweetTexts.Where(kvp => tweetIds.Contains(kvp.Key)).ToDictionary(
                                   kvp => kvp.Key,
                                   kvp => kvp.Value)
                       };

            result.Tweets = Tweet.FromCrowdData(result);
            result.Workers = Worker.FromCrowdData(result);

            return result;
        }

        /// <inheritdoc />
        public override CrowdData RestrictDataToSingleTweet(string tweetId)
        {
            var baseResult = base.RestrictDataToSingleTweet(tweetId);

            var result = new CrowdDataWithText
                             {
                                 CrowdLabels = baseResult.CrowdLabels,
                                 GoldLabels = baseResult.GoldLabels,
                                 TweetTexts =
                                     this.TweetTexts.Where(kvp => kvp.Key == tweetId).ToDictionary(
                                         kvp => kvp.Key,
                                         kvp => kvp.Value)
                             };

            result.Tweets = Tweet.FromCrowdData(result);
            result.Workers = Worker.FromCrowdData(result);

            return result;
        }
    }
}
