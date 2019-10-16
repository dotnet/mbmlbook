// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace HarnessingTheCrowd
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ML.Probabilistic.Algorithms;
    using Microsoft.ML.Probabilistic.Collections;
    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Math;
    using Microsoft.ML.Probabilistic.Models;

    /// <summary>
    /// Base class for model classes.
    /// </summary>
    public abstract class ModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelBase"/> class.
        /// </summary>
        protected ModelBase(InferenceEngine engine)
        {
            // Set up inference engine
            this.Engine = new InferenceEngine();
            this.Engine.SetTo(engine);
        }

        /// <summary>
        /// Gets the name of the model.
        /// </summary>
        public virtual string Name => this.GetType().Name;

        /// <summary>
        /// The number of label values.
        /// </summary>
        public int LabelValueCount => this.Labels?.SizeAsInt ?? 0;


        /// <summary>
        /// The number of gold labels.
        /// </summary>
        public int GoldLabelCount => !(this.NumberOfGoldLabels is null) && this.NumberOfGoldLabels.IsObserved ? this.NumberOfGoldLabels.ObservedValue : 0;

        /// <summary>
        /// The number of tweets.
        /// </summary>
        public int TweetCount => this.Tweets?.SizeAsInt ?? 0;

        /// <summary>
        /// The number of workers.
        /// </summary>
        public int WorkerCount => ((Variable<int>)this.Workers?.Size)?.ObservedValue ?? 0;

        /// <summary>
        /// Gets or sets a value indicating whether the worker labels should be
        /// aggregated during training to provide a gold label
        /// </summary>
        public bool AggregateWorkerLabels { get; protected set; } = false;

        /// <summary>
        /// Gets or sets the range for the number of tweets.
        /// </summary>
        protected Range Tweets { get; set; }

        /// <summary>
        /// Gets or sets the range for the number of workers.
        /// </summary>
        protected Range Workers { get; set; }

        /// <summary>
        /// Gets or sets the range for the number of label values
        /// </summary>
        protected Range Labels { get; set; }


        /// <summary>
        /// Gets or sets the range for the gold labels
        /// </summary>
        protected Range GoldLabels { get; set; }

        /// <summary>
        /// Gets or sets the jagged range indexed by workers and tweets.
        /// </summary>
        protected Range WorkerJudgment { get; set; }

        /// <summary>
        /// Gets or sets the worker count random variable.
        /// </summary>
        protected Variable<int> NumberOfWorkers { get; set; }

        /// <summary>
        /// Gets or sets the random variable for the number of gold labels.
        /// </summary>
        protected Variable<int> NumberOfGoldLabels { get; set; }

        /// <summary>
        /// Gets or sets the random variable for the indices of gold labels.
        /// </summary>
        protected VariableArray<int> GoldLabelIndices { get; set; }

        /// <summary>
        /// Gets or sets the random variable for the array of gold label values.
        /// </summary>
        protected VariableArray<int> GoldLabel { get; set; }

        /// <summary>
        /// Gets or sets the true label variable.
        /// </summary>
        protected VariableArray<int> TrueLabel { get; set; }

        /// <summary>
        /// Gets or sets the worker judgment count variable.
        /// </summary>
        protected VariableArray<int> WorkerJudgmentCount { get; set; }

        /// <summary>
        /// Gets or sets the worker judged tweet index variable.
        /// </summary>
        protected VariableArray<VariableArray<int>, int[][]> WorkerJudgedTweetIndex { get; set; }

        /// <summary>
        /// Gets or sets the worker label variable.
        /// </summary>
        protected VariableArray<VariableArray<int>, int[][]> WorkerLabel { get; set; }

        /// <summary>
        /// Gets or sets the background label probability variable.
        /// </summary>
        protected Variable<Vector> ProbLabel { get; set; }

        /// <summary>
        /// Gets or sets the evidence variable.
        /// </summary>
        protected Variable<bool> Evidence { get; set; }

        /// <summary>
        /// Gets or sets the background label probability prior distribution variable.
        /// </summary>
        protected Variable<Dirichlet> ProbLabelPrior { get; set; }

        /// <summary>
        /// Gets or sets the inference engine.
        /// </summary>
        protected InferenceEngine Engine { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether has the model calculates evidence.
        /// </summary>
        protected bool HasEvidence { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether has the model processes gold labels.
        /// </summary>
        protected bool HasGoldLabels { get; set; }

        /// <summary>
        /// Whether the model has converged.
        /// </summary>
        /// <param name="values">
        ///     The values on which to check convergence.
        /// </param>
        /// <param name="numToCheck">Number of values to check.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool HasConverged(List<double> values, int numToCheck = 3, double tolerance = 1e-5)
        {
            if (values.Count < numToCheck)
            {
                return false;
            }

            values = values.Skip(values.Count - numToCheck).ToList();

            var minVal = values.Min();
            var maxVal = values.Max();

            return maxVal - minVal < tolerance;
        }

        /// <summary>
        /// Creates the model.
        /// </summary>
        /// <param name="numTweets">
        /// The number of tweets.
        /// </param>
        /// <param name="numClasses">
        /// The num classes.
        /// </param>
        public virtual void CreateModelStub(int numTweets, int numClasses, bool withGoldLabels = false)
        {
            // Ranges and indexing
            this.NumberOfWorkers = Variable.New<int>().Named("numberOfWorkers");
            this.Tweets = new Range(numTweets).Named("tweets");
            this.Labels = new Range(numClasses).Named("labels");
            this.Workers = new Range(this.NumberOfWorkers).Named("workers");

            this.WorkerJudgmentCount = Variable.Array<int>(this.Workers).Named("workerJudgmentCount");
            this.WorkerJudgment = new Range(this.WorkerJudgmentCount[this.Workers]).Named("workerJudgment");
            this.WorkerJudgedTweetIndex = Variable.Array(Variable.Array<int>(this.WorkerJudgment), this.Workers).Named("workerJudgedTweetIndex");
            this.WorkerJudgedTweetIndex.SetValueRange(this.Tweets);

            // Truth variable
            this.ProbLabelPrior = Variable.New<Dirichlet>().Named("probLabelPrior");
            this.ProbLabel = Variable<Vector>.Random(this.ProbLabelPrior).Named("probLabel");
            this.ProbLabel.SetValueRange(this.Labels);
            this.TrueLabel = Variable.Array<int>(this.Tweets).Named("trueLabel");
            this.TrueLabel[this.Tweets] = Variable.Discrete(this.ProbLabel).ForEach(this.Tweets);

            // Gold labels
            this.HasGoldLabels = withGoldLabels;
            if (withGoldLabels)
            {
                this.NumberOfGoldLabels = Variable.New<int>().Named("numberOfGoldLabels");
                this.GoldLabels = new Range(this.NumberOfGoldLabels).Named("goldLabels");
                this.GoldLabelIndices = Variable.Array<int>(this.GoldLabels).Named("goldLabelIndices");
                this.GoldLabel = Variable.Subarray(this.TrueLabel, this.GoldLabelIndices).Named("goldLabel");
            }

            // Worker labels
            this.WorkerLabel = Variable.Array(Variable.Array<int>(this.WorkerJudgment), this.Workers).Named("workerLabel");
        }

        /// <summary>
        /// Observe the labels.
        /// </summary>
        /// <param name="workerLabel">
        /// The worker label.
        /// </param>
        /// <param name="workerJudgedTweetIndex">
        /// The worker judged tweet indices.
        /// </param>
        /// <param name="goldLabels">
        /// The gold labels index by tweet index.
        /// </param>
        public void ObserveLabels(int[][] workerLabel, int[][] workerJudgedTweetIndex, int?[] goldLabels = null)
        {
            this.ProbLabelPrior.ObservedValue = Dirichlet.Uniform(this.Labels.SizeAsInt);
            this.NumberOfWorkers.ObservedValue = workerLabel.Length;
            this.WorkerLabel.ObservedValue = workerLabel;
            this.WorkerJudgmentCount.ObservedValue = workerJudgedTweetIndex.Select(tweets => tweets.Length).ToArray();
            this.WorkerJudgedTweetIndex.ObservedValue = workerJudgedTweetIndex;

            if (this.HasGoldLabels)
            {
                if (goldLabels != null)
                {
                    var indicesOfGoldLabels = goldLabels.FindAllIndex(lab => lab.HasValue).ToArray();
                    this.NumberOfGoldLabels.ObservedValue = indicesOfGoldLabels.Length;
                    this.GoldLabelIndices.ObservedValue = indicesOfGoldLabels;
                    this.GoldLabel.ObservedValue = indicesOfGoldLabels.Select(idx => goldLabels[idx].Value).ToArray();
                }
                else
                {
                    this.NumberOfGoldLabels.ObservedValue = 0;
                    this.GoldLabelIndices.ObservedValue = new int[0];
                    this.GoldLabel.ObservedValue = new int[0];
                }
            }
        }

        /// <summary>
        /// Sets the default priors.
        /// </summary>
        public virtual void SetDefaultPriors()
        {
            this.ProbLabelPrior.ObservedValue = Dirichlet.Uniform(this.LabelValueCount);
        }

        /// <summary>
        /// Sets the priors from the posteriors.
        /// </summary>
        /// <param name="newWorkerToOldWorkerMap">
        /// The old Worker To New Worker Map.
        /// </param>
        /// <param name="newWordToOldWordMap">
        /// The old word to new word map.
        /// </param>
        /// <param name="modelPosteriors">
        /// The model Posteriors.
        /// </param>
        public virtual void SetPriorsFromPosteriors(int[] newWorkerToOldWorkerMap, int[] newWordToOldWordMap, ModelPosteriors modelPosteriors)
        {
            this.SetDefaultPriors();
            this.ProbLabelPrior.ObservedValue = modelPosteriors.BackgroundLabelProb;
        }

        /// <summary>
        /// Creates the model.
        /// </summary>
        /// <param name="numTweets">
        /// The number of tweets.
        /// </param>
        /// <param name="numClasses">
        /// The number of classes.
        /// </param>
        /// <param name="numVocab">
        /// The number of vocabulary terms.
        /// </param>
        /// <param name="withEvidence">
        /// The with Evidence.
        /// </param>
    public abstract void CreateModel(int numTweets, int numClasses, int numVocab = 0, bool withEvidence = true, bool withGoldLabels = false);

        /// <summary>
        /// The infer posteriors.
        /// </summary>
        /// <param name="workerLabel">
        ///     The worker label.
        /// </param>
        /// <param name="workerJudgedTweetIndex">
        ///     The worker judged tweet indices.
        /// </param>
        /// <param name="words">
        ///     The words.
        /// </param>
        /// <param name="wordCounts">
        ///     The word counts.
        /// </param>
        /// <param name="newWorkerToOldWorkerMap">
        ///     The mapping from validation worker indices to training worker indices.
        /// </param>
        /// <param name="newWordToOldWordMap">
        ///     The mapping from validation word indices to training word indices.
        /// </param>
        /// <param name="goldLabels">
        ///     The gold labels (training only).
        /// </param>
        /// <param name="oldPosteriors">
        ///     The model posteriors from training.
        /// </param>
        /// <param name="numIterations">
        ///     The number of iterations.
        /// </param>
        /// <returns>
        /// The <see cref="ModelPosteriors"/>.
        /// </returns>
        public abstract ModelPosteriors InferPosteriors(
            int[][] workerLabel,
            int[][] workerJudgedTweetIndex,
            int[][] words = null,
            int[] wordCounts = null,
            int[] newWorkerToOldWorkerMap = null,
            int[] newWordToOldWordMap = null,
            int?[] goldLabels = null,
            ModelPosteriors oldPosteriors = null,
            int numIterations = 20);

        /// <summary>
        /// The honest worker model posteriors class.
        /// </summary>
        [Serializable]
        public class ModelPosteriors
        {
            /// <summary>
            /// Gets the inferred probabilities that generate the true labels of all the tweets.
            /// </summary>
            public Dirichlet BackgroundLabelProb { get; set; }

            /// <summary>
            /// Gets the inferred probabilities of the true label of each tweet.
            /// </summary>
            public Discrete[] TrueLabel { get; set; }

            /// <summary>
            /// Gets the inferred model evidence.
            /// </summary>
            public Bernoulli Evidence { get; set; }
        }
    }
}
