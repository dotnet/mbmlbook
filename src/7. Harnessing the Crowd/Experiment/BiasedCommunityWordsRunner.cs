// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace HarnessingTheCrowd
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Math;

    /// <summary>
    /// Runs the biased community words  model.
    /// </summary>
    [Serializable]
    [DataContract]
    public class BiasedCommunityWordsRunner : BiasedCommunityModelRunner
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BiasedCommunityWordsRunner"/> class. 
        /// </summary>
        /// <param name="dataMapping">
        /// The data mapping.
        /// </param>
        /// <param name="model">
        /// The model.
        /// </param>
        /// <param name="trainingRunner">
        /// The training runner.
        /// </param>
        public BiasedCommunityWordsRunner(CrowdDataWithTextMapping dataMapping, BiasedCommunityWordsModel model, ModelRunnerBase trainingRunner = null)
            : base(dataMapping, model, trainingRunner)
        {
        }

        /// <summary>
        /// Gets or sets the posterior of probability of words per class.
        /// </summary>
        [DataMember]
        public Dictionary<int, Dirichlet> ProbWords { get; set; }

        /// <summary>
        /// Gets or sets the posterior of log probability of words per class.
        /// </summary>
        [DataMember]
        public Dictionary<int, Dictionary<string, double>> LogProbWord { get; set; }

        /// <summary>
        /// Gets or sets the background log probability of words.
        /// </summary>
        [DataMember]
        public Dictionary<string, double> BackgroundLogProbWord { get; set; }

        /// <summary>
        /// Gets or sets the relative log prob of a word in each class.
        /// </summary>
        [DataMember]
        public Dictionary<int, Dictionary<string, double>> RelativeLogProbWord { get; set; }

        /// <summary>
        /// Gets the log probabilities of words per class in descending order.
        /// </summary>
        public Dictionary<int, List<KeyValuePair<string, double>>> OrderedLogProbWord
        {
            get
            {
                return this.LogProbWord?.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value
                    .Select(
                        kvp1 => new KeyValuePair<string, double>(
                            kvp1.Key,
                            kvp1.Value)).OrderByDescending(x => x.Value).ToList());
            }
        }

        /// <summary>
        /// Gets the background log probabilities in descending order.
        /// </summary>
        public List<KeyValuePair<string, double>> OrderedBackgroundLogProbWord
        {
            get
            {
                return this.BackgroundLogProbWord
                        ?.Select(
                            kvp => new KeyValuePair<string, double>(
                                kvp.Key,
                                kvp.Value)).OrderByDescending(x => x.Value).ToList();
            }
        }

        /// <summary>
        /// Gets the log probabilities of words (relative to the background) per class in descending order.
        /// </summary>
        public Dictionary<int, List<KeyValuePair<string, double>>> OrderedRelativeLogProbWord
        {
            get
            {
                return this.RelativeLogProbWord?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                        ?.Select(
                            kvp1 => new KeyValuePair<string, double>(
                                kvp1.Key,
                                kvp1.Value)).OrderByDescending(x => x.Value).ToList());
            }
        }

        /// <summary>
        /// Gets or sets the number of words per class to print out.
        /// </summary>
        public int NumberOfWordsToPrint { get; set; } = 30;

        /// <inheritdoc />
        protected override void ClearResults()
        {
            base.ClearResults();
            this.ProbWords = null;
            this.BackgroundLogProbWord = null;
            this.LogProbWord = null;
            this.RelativeLogProbWord = null;
        }

        /// <inheritdoc />
        protected override void UpdateResults()
        {
            if (!(this.DataMapping is CrowdDataWithTextMapping wordsMapping))
            {
                return;
            }

            if (this.Posteriors is BiasedCommunityWordsModel.BiasedCommunityWordsPosteriors modelPosteriors)
            {
                if (modelPosteriors?.ProbWordPosterior != null)
                {
                    this.ProbWords = new Dictionary<int, Dirichlet>(modelPosteriors.ProbWordPosterior.Length);
                    for (var c = 0; c < modelPosteriors?.ProbWordPosterior.Length; c++)
                    {
                        this.ProbWords[this.DataMapping.LabelIndexToValue[c]] = modelPosteriors.ProbWordPosterior[c];
                    }
                }

                var numberOfWords = wordsMapping.CorpusInfo.NumberOfWords;
                var numClasses = this.ProbWords.Count;

                // Convert to log probs
                var logProbWords = this.ProbWords.ToDictionary(
                    kvp => kvp.Key,
                    kvp =>
                        {
                            var mean = kvp.Value.GetMean();
                            mean.SetToFunction(mean, Math.Log);
                            return mean;
                        });

                // The background log probs
                var backgroundAverageLogProbWords =
                    logProbWords.Aggregate(Vector.Zero(numberOfWords), (vec, kvp) => vec + kvp.Value)
                    / ((double)numClasses);

                // Make human readable.
                this.BackgroundLogProbWord = backgroundAverageLogProbWords
                    .Select(
                        (value, index) => new KeyValuePair<string, double>(
                            wordsMapping.CorpusInfo.Vocabulary[index],
                            value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                this.LogProbWord = logProbWords.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                        .Select(
                            (value, index) => new KeyValuePair<string, double>(
                                wordsMapping.CorpusInfo.Vocabulary[index],
                                value)).ToDictionary(kvp1 => kvp1.Key, kvp1 => kvp1.Value));

                this.RelativeLogProbWord = this.LogProbWord.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToDictionary(
                        kvp1 => kvp1.Key,
                        kvp1 => kvp1.Value - this.BackgroundLogProbWord[kvp1.Key]));
            }

            base.UpdateResults();
        }
    }
}