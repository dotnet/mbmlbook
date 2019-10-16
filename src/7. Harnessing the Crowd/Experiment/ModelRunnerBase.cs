// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace HarnessingTheCrowd
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    using Microsoft.ML.Probabilistic.Collections;
    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Math;
    using Microsoft.ML.Probabilistic.Utilities;

    /// <summary>
    /// Base class for running models.
    /// </summary>
    [Serializable]
    [DataContract]
    public abstract class ModelRunnerBase : RunnerBase
    {
        /// <inheritdoc />
        /// <param name="dataMapping">
        /// The data mapping
        /// </param>
        /// <param name="model">
        /// The model.
        /// </param>
        /// <param name="trainingRunner">
        /// The training Runner. This should be null if
        /// (a) we are running training, or
        /// (b) we are running validation and the model has no training
        /// </param>
        protected ModelRunnerBase(CrowdDataMapping dataMapping, ModelBase model, ModelRunnerBase trainingRunner = null)
            : base(dataMapping)
        {
            this.Model = model;
            this.TrainingRunner = trainingRunner;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelRunnerBase"/> class.
        /// </summary>
        protected ModelRunnerBase()
        {
        }

        /// <summary>
        /// Gets the model.
        /// </summary>
        public ModelBase Model { get; internal set; }

        /// <summary>
        /// Gets or sets the training Runner. This is null if
        /// (a) we are running training, or
        /// (b) we are running validation and the model has no training
        /// </summary>
        public ModelRunnerBase TrainingRunner { get; set; }
        
        /// <summary>
        /// Gets or sets the probabilities that generate the true label of all the tweets.
        /// </summary>
        [DataMember]
        public Dirichlet BackgroundLabelProb { get; set; }

        /// <summary>
        /// Gets or sets the model evidence.
        /// </summary>
        [DataMember]
        public Bernoulli ModelEvidence { get; set; }

        /// <summary>
        /// Gets or sets the posterior of the true label for each tweet.
        /// </summary>
        [DataMember]
        public Dictionary<string, Discrete> TrueLabel { get; set; }

        /// <summary>
        /// Gets or sets the predictive probabilities of the labels produced by each worker.
        /// </summary>
        [DataMember]
        public Dictionary<string, Dictionary<string, Discrete>> WorkerPrediction { get; set; }

        /// <summary>
        /// Gets or sets the average log probability of the current true label predictions.
        /// </summary>
        [DataMember]
        public double AverageLogProbability { get; set; }

        /// <summary>
        /// Gets or sets the average log probability of the current true label predictions.
        /// </summary>
        [DataMember]
        public ModelBase.ModelPosteriors Posteriors { get; set; }

        /// <inheritdoc />
        public override Dictionary<Metric, object> RunModel(int numIterations = 20, bool useGoldLabels = false)
        {
            var trainingMapping = this.TrainingRunner?.DataMapping;
            var validating = trainingMapping != null && this.GetType() == this.TrainingRunner.GetType();
            var posteriorsFromTrainingRun = this.TrainingRunner?.Posteriors;
            var thisMapping = this.DataMapping;

            // Create model
            var wordsDataMapping = thisMapping as CrowdDataWithTextMapping;
            var numVocab = wordsDataMapping?.WordCount ?? 0;
            this.Model.CreateModel(validating ? 1 : thisMapping.TweetCount, this.DataMapping.LabelCount, numVocab, !validating, useGoldLabels);
            this.ClearResults();

            if (validating)
            {
                // Validation
                var trainingWordsMapping = trainingMapping as CrowdDataWithTextMapping;
                var thisWordsMapping = this.DataMapping as CrowdDataWithTextMapping;
                var trainingCorpusInfo = trainingWordsMapping?.CorpusInfo;
                var thisCorpusInfo = thisWordsMapping?.CorpusInfo;

                // Mapping between word indices in the validation mapping to word indices in the training mapping.
                int[] wordValidationIndexToWordTrainingIndex = null;
                if (trainingCorpusInfo != null && thisCorpusInfo != null)
                {
                    wordValidationIndexToWordTrainingIndex = thisCorpusInfo.Vocabulary.Select(
                        word => trainingCorpusInfo.VocabularyToVocabularyIndex.ContainsKey(word)
                                    ? trainingCorpusInfo.VocabularyToVocabularyIndex[word]
                                    : -1).ToArray();
                }

                this.Posteriors = new ModelBase.ModelPosteriors { TrueLabel = new Discrete[thisMapping.TweetCount] };
                for (var tweetIndex = 0; tweetIndex < thisMapping.TweetCount; tweetIndex++)
                {
                    var tweetId = thisMapping.TweetIds[tweetIndex];
                    var currentDataMapping = thisMapping.RestrictToSingleTweet(tweetId);
                    var workerValidationIndexToWorkerTrainingIndex = this.Model.AggregateWorkerLabels
                                                                         ? new int[0]
                                                                         : currentDataMapping.WorkerIndexToId.Select(
                                                                             id => trainingMapping.WorkerIdToIndex
                                                                                       .ContainsKey(id)
                                                                                       ? trainingMapping
                                                                                           .WorkerIdToIndex[id]
                                                                                       : -1).ToArray();
                    var labelsPerWorkerIndex = this.Model.AggregateWorkerLabels ? new int[0][] : currentDataMapping.GetLabelsPerWorkerIndex(currentDataMapping.Data);
                    var tweetIndicesPerWorkerIndex = this.Model.AggregateWorkerLabels ? new int[0][] : currentDataMapping.GetTweetIndicesPerWorkerIndex(currentDataMapping.Data);
                    var currentWordsDataMapping = currentDataMapping as CrowdDataWithTextMapping;
                    var wordIndicesPerTweetIndex = currentWordsDataMapping?.WordIndicesPerTweetIndex ?? null;
                    var wordCountsPerTweetIndex = currentWordsDataMapping?.WordCountsPerTweetIndex ?? null;

                    var posteriors = this.Model.InferPosteriors(
                        labelsPerWorkerIndex,
                        tweetIndicesPerWorkerIndex,
                        wordIndicesPerTweetIndex,
                        wordCountsPerTweetIndex,
                        workerValidationIndexToWorkerTrainingIndex,
                        wordValidationIndexToWordTrainingIndex,
                        null,
                        posteriorsFromTrainingRun,
                       numIterations);
                    this.Posteriors.TrueLabel[tweetIndex] = posteriors.TrueLabel[0];
                }
            }
            else
            {
                var labelsPerWorkerIndex = thisMapping.GetLabelsPerWorkerIndex(thisMapping.Data);
                var tweetIndicesPerWorkerIndex = thisMapping.GetTweetIndicesPerWorkerIndex(thisMapping.Data);
                var wordIndicesPerTweetIndex = wordsDataMapping?.WordIndicesPerTweetIndex ?? null;
                var wordCountsPerTweetIndex = wordsDataMapping?.WordCountsPerTweetIndex ?? null;

                // Training
                int?[] goldLabels = null;
                if (this.Model.AggregateWorkerLabels)
                {
                    var labelHistogram = Util.ArrayInit(this.Model.TweetCount, i => Util.ArrayInit(this.Model.LabelValueCount, j => 0));
                    for (var i = 0; i < labelsPerWorkerIndex.Length; i++)
                    {
                        for (var j = 0; j < labelsPerWorkerIndex[i].Length; j++)
                        {
                            labelHistogram[tweetIndicesPerWorkerIndex[i][j]][labelsPerWorkerIndex[i][j]]++;
                        }
                    }

                    goldLabels = labelHistogram.Select(hist => hist.IndexOf(hist.Max())).Cast<int?>().ToArray();
                }
                else if (useGoldLabels)
                {
                     goldLabels = Util.ArrayInit(
                        thisMapping.TweetCount,
                        i => thisMapping.Data.GoldLabels.ContainsKey(thisMapping.TweetIds[i])
                                 ? (int?)thisMapping.Data.GoldLabels[thisMapping.TweetIds[i]]
                                 : (int?)null);
                }

                // Run model inference
                this.Posteriors = this.Model.InferPosteriors(
                    this.Model.AggregateWorkerLabels ? new int[0][] : labelsPerWorkerIndex,
                    this.Model.AggregateWorkerLabels ? new int[0][] : tweetIndicesPerWorkerIndex,
                    wordIndicesPerTweetIndex,
                    wordCountsPerTweetIndex,
                    null,
                    null,
                    goldLabels,
                    posteriorsFromTrainingRun,
                    numIterations);
            }

            this.UpdateResults();
            return this.UpdateMetrics();
        }

        /// <inheritdoc />
        protected override Dictionary<Metric, object> UpdateMetrics()
        {
            var result = base.UpdateMetrics();
            var nlpdThreshold = -Math.Log(0.001);
            var logProb = 0.0;
            var count = 0;
            foreach (var kvp in this.GoldLabels)
            {
                var goldLabel = kvp.Value;
                if (this.TrueLabel.ContainsKey(kvp.Key))
                {
                    var nlp = this.TrueLabel[kvp.Key].GetLogProb(goldLabel);
                    if (nlp > nlpdThreshold)
                    {
                        nlp = nlpdThreshold;
                    }

                    logProb += nlp;
                    count++;
                }
            }

            this.AverageLogProbability = count > 0 ? logProb / count : double.NegativeInfinity;
            result[Metric.AverageLogProb] = this.AverageLogProbability;
            return result;
        }

        /// <summary>
        /// Clear the results.
        /// </summary>
        protected override void ClearResults()
        {
            base.ClearResults();
            this.BackgroundLabelProb = Dirichlet.Uniform(this.DataMapping.LabelCount);
            this.WorkerPrediction = new Dictionary<string, Dictionary<string, Discrete>>();
            this.TrueLabel = new Dictionary<string, Discrete>();
            this.AverageLogProbability = double.NegativeInfinity;
            this.ModelEvidence = new Bernoulli(0.5);
        }

        /// <inheritdoc />
        protected override void SetPredictions()
        {
            var result = new Dictionary<string, int>();
            foreach (var kvp in this.TrueLabel)
            {
                var probs = this.TrueLabel[kvp.Key].GetProbs();
                var max = probs.Max();
                var predictedLabels = probs.Select((p, i) => new { prob = p, idx = i }).Where(a => a.prob == max)
                    .Select(a => a.idx).ToArray();

                var predictedLabel = predictedLabels.Length == 1
                                         ? predictedLabels[0]
                                         : predictedLabels[Rand.Int(predictedLabels.Length)];

                result[kvp.Key] = this.DataMapping.LabelIndexToValue[predictedLabel];
            }

            this.Predictions = result;
        }

        /// <summary>
        /// Update the results.
        /// </summary>
        protected virtual void UpdateResults()
        {
            // Update results for base model.
            this.BackgroundLabelProb = this.Posteriors.BackgroundLabelProb;

            if (this.Posteriors.TrueLabel != null)
            {
                for (var t = 0; t < this.Posteriors.TrueLabel.Length; t++)
                {
                    this.TrueLabel[this.DataMapping.TweetIds[t]] = this.Posteriors.TrueLabel[t];
                }
            }

            this.ModelEvidence = this.Posteriors.Evidence;

            // Set the point predictions.
            this.SetPredictions();
        }
    }
}
