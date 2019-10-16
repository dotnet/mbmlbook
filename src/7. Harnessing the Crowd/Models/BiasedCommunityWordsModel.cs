// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.ML.Probabilistic.Algorithms;

namespace HarnessingTheCrowd
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ML.Probabilistic.Collections;
    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Factors;
    using Microsoft.ML.Probabilistic.Math;
    using Microsoft.ML.Probabilistic.Models;
    using Microsoft.ML.Probabilistic.Utilities;

    /// <summary>
    /// The biased community words model
    /// </summary>
    public class BiasedCommunityWordsModel : BiasedCommunityModel
    {
        /// <summary>
        /// Whether to use a fractional power plate. This activates the
        /// <see cref="LikelihoodExponent"/>. 
        /// </summary>
        public static bool UseRepeatFactor = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="BiasedCommunityWordsModel"/> class.
        /// </summary>
        public BiasedCommunityWordsModel(InferenceEngine engine) : base(engine)
        {
            this.Engine.Compiler.UseLocals = true;
        }

        /// <inheritdoc />
        public override string Name => $"Naive Words and Community ({this.NumberOfCommunities})";

        /// <summary>
        /// Gets or sets the initial count for the prob word prior.
        /// </summary>
        public double ProbWordInitialCount { get; set; } = 10.0;

        /// <summary>
        /// Gets the vocabulary size.
        /// </summary>
        public int VocabularySize => this.WordsInVocabulary?.SizeAsInt ?? 0;

        /// <summary>
        /// Gets or sets the range for words.
        /// </summary>
        protected Range WordsInVocabulary { get; set; }

        /// <summary>
        /// Gets or sets the jagged range indexed by tweet and word.
        /// </summary>
        protected Range Words { get; set; }

        /// <summary>
        /// Gets or sets the prob word.
        /// </summary>
        protected VariableArray<Vector> ProbWords { get; set; }

        /// <summary>
        /// Gets or sets the words variable.
        /// </summary>
        protected VariableArray<VariableArray<int>, int[][]> Word { get; set; }

        /// <summary>
        /// Gets or sets the word count variable.
        /// </summary>
        protected VariableArray<int> WordCount { get; set; }

        /// <summary>
        /// Gets or sets the exponent for the tweet's likelihood. This
        /// compensates for the Naive Bayes assumption that observations
        /// are independent.
        /// </summary>
        protected VariableArray<double> LikelihoodExponent { get; set; }

        /// <summary>
        /// Gets or sets the prob word prior variable.
        /// </summary>
        protected VariableArray<Dirichlet> ProbWordsPrior { get; set; }

        /// <inheritdoc />
        public override void CreateModel(int numTweets, int numClasses, int numVocab = 0, bool withEvidence = true, bool withGoldLabels = false)
        {
            IfBlock block = null;
            if (withEvidence)
            {
                this.Evidence = Variable.Bernoulli(0.5).Named("evidence");
                block = Variable.If(this.Evidence);
            }

            // Biased community model
            base.CreateModel(numTweets, numClasses, numVocab, false, withGoldLabels);

            // Add in the word generation conditioned on true label variable.
            this.WordsInVocabulary = new Range(numVocab).Named("wordsInVocabulary");
            this.ProbWords = Variable.Array<Vector>(this.Labels).Named("probWords");
            this.ProbWords.SetValueRange(this.WordsInVocabulary);
            this.ProbWords.SetSparsity(Sparsity.Sparse);
            this.WordCount = Variable.Array<int>(this.Tweets).Named("wordCount");
            this.Words = new Range(this.WordCount[this.Tweets]).Named("words");
            this.Word = Variable.Array(Variable.Array<int>(this.Words), this.Tweets).Named("word");

            this.ProbWordsPrior = Variable.Array<Dirichlet>(this.Labels).Named("probWordsPrior");
            this.ProbWords[this.Labels] = Variable<Vector>.Random(this.ProbWordsPrior[this.Labels]);
            this.LikelihoodExponent = Variable.Array<double>(this.Tweets).Named(nameof(this.LikelihoodExponent));

            using (Variable.ForEach(this.Tweets))
            {
                RepeatBlock repeatBlock = null;
                if (UseRepeatFactor)
                {
                    repeatBlock = Variable.Repeat(this.LikelihoodExponent[this.Tweets]);
                }

                using (Variable.Switch(this.TrueLabel[this.Tweets]))
                {
                    this.Word[this.Tweets][this.Words] = Variable.Discrete(this.ProbWords[this.TrueLabel[this.Tweets]]).ForEach(this.Words);
                }

                if (UseRepeatFactor)
                {
                    repeatBlock?.CloseBlock();
                }
            }

            if (withEvidence)
            {
                block.CloseBlock();
            }

            this.HasEvidence = withEvidence;
        }

        /// <inheritdoc />
        public override void SetDefaultPriors()
        {
            base.SetDefaultPriors();
            this.ProbWordsPrior.ObservedValue = Util.ArrayInit(this.LabelValueCount, i => Dirichlet.Symmetric(this.VocabularySize, this.ProbWordInitialCount));
        }

        /// <inheritdoc />
        public override void SetPriorsFromPosteriors(
            int[] newWorkerToOldWorkerMap,
            int[] newWordToOldWordMap,
            ModelPosteriors modelPosteriors)
        {
            base.SetPriorsFromPosteriors(newWorkerToOldWorkerMap, newWordToOldWordMap, modelPosteriors);
            var biasedCommunityWordsModelPosteriors = (BiasedCommunityWordsPosteriors)modelPosteriors;
            var probWordPosteriors = biasedCommunityWordsModelPosteriors.ProbWordPosterior;
            AssertWhenDebugging.IsTrue(this.LabelValueCount == probWordPosteriors.Length);
            this.ProbWordsPrior.ObservedValue = Util.ArrayInit(this.LabelValueCount, i => Dirichlet.Symmetric(this.VocabularySize, this.ProbWordInitialCount));

            for (var labIndex = 0; labIndex < probWordPosteriors.Length; labIndex++)
            {
                var probWordPosterior = probWordPosteriors[labIndex];
                var oldPseudoCounts = probWordPosterior.PseudoCount;
                var newPseudoCounts = Vector.Constant(this.VocabularySize, this.ProbWordInitialCount, oldPseudoCounts.Sparsity);
                for (var wordIndex = 0; wordIndex < newWordToOldWordMap.Length; wordIndex++)
                {
                    var oldIdx = newWordToOldWordMap[wordIndex];
                    if (oldIdx >= 0)
                    {
                        newPseudoCounts[wordIndex] = oldPseudoCounts[oldIdx];
                    }
                }

                this.ProbWordsPrior.ObservedValue[labIndex] = new Dirichlet(newPseudoCounts);
            }
        }

        /// <inheritdoc />
        public override ModelPosteriors InferPosteriors(
            int[][] workerLabel,
            int[][] workerJudgedTweetIndex,
            int[][] words = null,
            int[] wordCounts = null,
            int[] newWorkerToOldWorkerMap = null,
            int[] newWordToOldWordMap = null,
            int?[] goldLabels = null,
            ModelPosteriors oldPosteriors = null,
            int numIterations = 20)
        {
            this.ObserveLabels(workerLabel, workerJudgedTweetIndex, goldLabels);
            this.ObserveWords(words, wordCounts);
            if (newWorkerToOldWorkerMap == null || oldPosteriors == null)
            {
                this.SetDefaultPriors();
            }
            else
            {
                this.SetPriorsFromPosteriors(newWorkerToOldWorkerMap, newWordToOldWordMap, oldPosteriors);
            }

            // Initialize messages
            var discreteUniform = Discrete.Uniform(this.NumberOfCommunities);
            this.WorkerCommunityInitializer.ObservedValue = Distribution<int>.Array(Util.ArrayInit(workerLabel.Length, w => Discrete.PointMass(discreteUniform.Sample(), this.NumberOfCommunities)));

            var posteriors = new BiasedCommunityWordsPosteriors();
            var evidences = new List<double>();
            try
            {
                for (var it = 1; it <= numIterations; it++)
                {
                    this.Engine.NumberOfIterations = it;
                    posteriors.TrueLabel = this.Engine.Infer<Discrete[]>(this.TrueLabel);
                    posteriors.CommunityCpt = this.Engine.Infer<Dirichlet[][]>(this.ProbWorkerLabel);
                    posteriors.WorkerCommunities = this.Engine.Infer<Discrete[]>(this.Community);
                    posteriors.BackgroundLabelProb = this.Engine.Infer<Dirichlet>(this.ProbLabel);
                    posteriors.ProbWordPosterior = this.Engine.Infer<Dirichlet[]>(this.ProbWords);
                    if (this.HasEvidence)
                    {
                        posteriors.Evidence = this.Engine.Infer<Bernoulli>(this.Evidence);
                        Console.WriteLine($"Iteration {it} log evidence:\t{posteriors.Evidence.LogOdds:0.0000}");
                        evidences.Add(posteriors.Evidence.LogOdds);
                        if (ModelBase.HasConverged(evidences))
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return posteriors;
        }

        /// <summary>
        /// Observe the words.
        /// </summary>
        /// <param name="words">
        /// The words.
        /// </param>
        /// <param name="wordCounts">
        /// The word counts.
        /// </param>
        protected virtual void ObserveWords(int[][] words, int[] wordCounts)
        {
            this.Word.ObservedValue = words;
            this.WordCount.ObservedValue = wordCounts;

            if (UseRepeatFactor)
            {
                this.LikelihoodExponent.ObservedValue = wordCounts.Select(
                    cnt => cnt == 0 ? 1.0 : 1.0 / cnt).ToArray();
            }
        }

        /// <summary>
        /// Biased community model posterior class.
        /// </summary>
        [Serializable]
        public class BiasedCommunityWordsPosteriors : BiasedCommunityModelPosteriors
        {
            /// <summary>
            /// Gets the Dirichlet posteriors of the word probabilities for each true label value.
            /// </summary>
            public Dirichlet[] ProbWordPosterior { get; internal set; }
        }
    }
}
