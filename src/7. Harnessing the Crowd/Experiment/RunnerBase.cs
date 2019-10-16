// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace HarnessingTheCrowd
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization;
    using Microsoft.ML.Probabilistic.Utilities;

    /// <summary>
    /// Base class for running models.
    /// </summary>
    [Serializable]
    [DataContract]
    public abstract class RunnerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunnerBase"/> class.
        /// </summary>
        /// <param name="dataMapping">
        ///     The mapping between data and indices.
        /// </param>
        protected RunnerBase(CrowdDataMapping dataMapping)
        {
            this.DataMapping = dataMapping;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RunnerBase"/> class.
        /// </summary>
        protected RunnerBase()
        {
        }

        /// <summary>
        /// The metric.choices of metric
        /// </summary>
        public enum Metric
        {
            /// <summary>
            /// The accuracy.
            /// </summary>
            Accuracy,

            /// <summary>
            /// Average log prob.
            /// </summary>
            AverageLogProb,

            /// <summary>
            /// The average recall.
            /// </summary>
            AverageRecall,

            /// <summary>
            /// The confusion matrix.
            /// </summary>
            ConfusionMatrix,

            /// <summary>
            /// The tweet matrix.
            /// </summary>
            TweetMatrix,

            /// <summary>
            /// The count.
            /// </summary>
            Count
        }

        /// <summary>
        /// Gets the gold labels.
        /// </summary>
        public Dictionary<string, int> GoldLabels => this.DataMapping?.Data.GoldLabels;

        /// <summary>
        /// Gets or sets the data mapping.
        /// </summary>
        public CrowdDataMapping DataMapping { get; set; }

        /// <summary>
        /// Gets or sets the predicted label for each tweet
        /// </summary>
        [DataMember]
        public Dictionary<string, int> Predictions { get; set; }

        /// <summary>
        /// Gets or sets the accuracy of the current true label predictions.
        /// </summary>
        [DataMember]
        public double Accuracy { get; set; }

        /// <summary>
        /// Gets or sets the average recall of the current true label predictions.
        /// </summary>
        [DataMember]
        public double AverageRecall { get; set; }

        /// <summary>
        /// Gets or sets the list of tweets indexed by (gold, inferred) labels.
        /// </summary>
        [DataMember]
        public List<Tweet>[,] TweetMatrix { get; set; }

        /// <summary>
        /// Gets or sets the confusion matrix of the predicted true labels against the gold labels
        /// The rows are the gold labels and the columns are the predicted labels.
        /// </summary>
        [DataMember]
        public double[,] ConfusionMatrix { get; set; }

        /// <summary>
        /// Gets the metrics.
        /// </summary>
        /// <param name="dataMapping">
        ///     The data mapping.
        /// </param>
        /// <param name="predictions">
        ///     The predictions.
        /// </param>
        /// <param name="trueLabels">
        ///     The true labels. If null, then the gold labels are used.
        /// </param>
        /// <returns>
        /// The dictionary of metric values.
        /// </returns>
        public static Dictionary<Metric, object> GetMetrics(CrowdDataMapping dataMapping, Dictionary<string, int> predictions, Dictionary<string, int> trueLabels = null)
        {
            var result = new Dictionary<Metric, object>();
            var labelCount = dataMapping.LabelCount;
            var data = dataMapping.Data as CrowdDataWithText;
            var confusionMatrix = Util.ArrayInit(labelCount, labelCount, (i, j) => 0.0);
            var tweetMatrix = Util.ArrayInit(labelCount, labelCount, (i, j) => new List<Tweet>());
            var correct = 0.0;

            var trueLabelCount = 0;
            if (trueLabels == null)
            {
                trueLabels = dataMapping.Data.GoldLabels;
            }

            foreach (var kvp in trueLabels)
            {
                var trueLabel = dataMapping.LabelValueToIndex[kvp.Value];
                if (predictions.ContainsKey(kvp.Key))
                {
                    trueLabelCount++;
                    var predictedLabel = dataMapping.LabelValueToIndex[predictions[kvp.Key]];

                    confusionMatrix[trueLabel, predictedLabel] = confusionMatrix[trueLabel, predictedLabel] + 1.0;
                    if (data != null)
                    {
                        if (data.Tweets.ContainsKey(kvp.Key))
                        {
                            tweetMatrix[trueLabel, predictedLabel].Add(data.Tweets[kvp.Key]);
                        }
                    }

                    if (trueLabel == predictedLabel)
                    {
                        correct++;
                    }
                }
            }

            result[Metric.Count] = trueLabelCount;
            result[Metric.Accuracy] = correct / trueLabelCount;
            result[Metric.ConfusionMatrix] = confusionMatrix;
            result[Metric.TweetMatrix] = tweetMatrix;

            // Average recall
            double sumRec = 0;
            for (var i = 0; i < labelCount; i++)
            {
                double classSum = 0;
                for (var j = 0; j < labelCount; j++)
                {
                    classSum += confusionMatrix[i, j];
                }

                sumRec += confusionMatrix[i, i] / classSum;
            }

            result[Metric.AverageRecall] = sumRec / labelCount;

            return result;
        }

        /// <summary>
        /// Runs the model.
        /// </summary>
        /// <param name="numIterations">
        ///     The number of iterations (ignored if not an iterative model).
        /// </param>
        /// <param name="useGoldLabels">Use gold labels.</param>
        /// <returns>
        /// The dictionary of metrics.
        /// </returns>
        public virtual Dictionary<Metric, object> RunModel(int numIterations = 20, bool useGoldLabels = false)
        {
            this.SetPredictions();
            return this.UpdateMetrics();
        }

        /// <summary>
        /// Gets the errors.
        /// </summary>
        /// <returns>
        /// The list of errors.
        /// </returns>
        public List<Error> GetErrors()
        {
            var result = new List<Error>();
            var labelValueToString = this.DataMapping.LabelValueToString;
            var data = this.DataMapping.Data as CrowdDataWithText;
            var texts = data?.TweetTexts;
            foreach (var kvp in this.GoldLabels)
            {
                var goldLabel = kvp.Value;
                if (this.Predictions.ContainsKey(kvp.Key))
                {
                    var predictedLabel = this.Predictions[kvp.Key];

                    if (goldLabel != predictedLabel)
                    {
                        result.Add(
                            new Error
                                {
                                    GoldLabel = labelValueToString[goldLabel],
                                    InferredLabel = labelValueToString[predictedLabel],
                                    TweetText = texts?[kvp.Key] ?? string.Empty
                                });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// The set predicted labels.
        /// </summary>
        protected abstract void SetPredictions();

        /// <summary>
        /// Updates the accuracy using the current results.
        /// </summary>
        /// <returns>
        /// The dictionary of metrics.
        /// </returns>
        protected virtual Dictionary<Metric, object> UpdateMetrics()
        {
            var metrics = GetMetrics(this.DataMapping, this.Predictions);
            this.Accuracy = (double)metrics[Metric.Accuracy];
            this.ConfusionMatrix = (double[,])metrics[Metric.ConfusionMatrix];
            this.TweetMatrix = (List<Tweet>[,])metrics[Metric.TweetMatrix];
            this.AverageRecall = (double)metrics[Metric.AverageRecall];
            return metrics;
        }

        /// <summary>
        /// Clear the results.
        /// </summary>
        protected virtual void ClearResults()
        {
            this.Predictions = new Dictionary<string, int>();
            this.Accuracy = 0.0;
            this.AverageRecall = 0.0;
            this.TweetMatrix = null;
            this.ConfusionMatrix = null;
        }

        /// <summary>
        /// Error information.
        /// </summary>
        public class Error
        {
            /// <summary>
            /// Gets the gold label of the error.
            /// </summary>
            public string GoldLabel { get; internal set; }

            /// <summary>
            /// Gets the inferred label of the error.
            /// </summary>
            public string InferredLabel { get; internal set; }

            /// <summary>
            /// Gets the tweet text for the error.
            /// </summary>
            public string TweetText { get; internal set; }

            /// <inheritdoc />
            public override string ToString()
            {
                return $@"{GoldLabel}/{InferredLabel}: {TweetText}";
            }
        }
    }
}
