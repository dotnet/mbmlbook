// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace HarnessingTheCrowd
{
    using System;
    using System.Collections.Generic;

    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Math;
    using Microsoft.ML.Probabilistic.Models;
    using Microsoft.ML.Probabilistic.Utilities;

    /// <summary>
    /// The honest worker model.
    /// </summary>
    public class HonestWorkerModel : ModelBase
    {
        public HonestWorkerModel(InferenceEngine engine) : base(engine)
        {
        }

        /// <inheritdoc />
        public override string Name => "Initial";

        /// <summary>
        /// Gets or sets the worker's ability variable.
        /// </summary>
        protected VariableArray<double> Ability { get; set; }

        /// <summary>
        /// Gets or sets the random guess probability vector variable.
        /// </summary>
        protected Variable<Vector> RandomGuessProbability { get; set; }

        /// <summary>
        /// Gets or sets the ability prior distribution variable.
        /// </summary>
        protected VariableArray<Beta> AbilityPrior { get; set; }

        /// <summary>
        /// Gets or sets the random guess prior distribution variable.
        /// </summary>
        protected Variable<Dirichlet> RandomGuessPrior { get; set; }

        /// <inheritdoc />
        public override void CreateModel(int numTweets, int numClasses, int numVocab = 0, bool withEvidence = true, bool withGoldLabels = false)
        {
            IfBlock block = null;
            if (withEvidence)
            {
                this.Evidence = Variable.Bernoulli(0.5).Named("evidence");
                block = Variable.If(this.Evidence);
            }

            this.CreateModelStub(numTweets, numClasses, withGoldLabels);

            // Honest worker
            this.AbilityPrior = Variable.Array<Beta>(this.Workers).Named("abilityPrior");
            this.Ability = Variable.Array<double>(this.Workers).Named("ability");
            this.Ability[this.Workers] = Variable<double>.Random(this.AbilityPrior[this.Workers]);

            this.RandomGuessPrior = Variable.New<Dirichlet>().Named("randomGuessPrior");
            this.RandomGuessProbability = Variable<Vector>.Random(this.RandomGuessPrior).Named("randomGuessProb");

            using (Variable.ForEach(this.Workers))
            {
                var trueLabels = Variable.Subarray(this.TrueLabel, this.WorkerJudgedTweetIndex[this.Workers]).Named("trueLabelSubarray");
                trueLabels.SetValueRange(this.Labels);
                using (Variable.ForEach(this.WorkerJudgment))
                {
                    var workerIsCorrect = Variable.Bernoulli(this.Ability[this.Workers]).Named("isCorrect");
                    using (Variable.If(workerIsCorrect))
                    {
                        var labelsEqual = (this.WorkerLabel[this.Workers][this.WorkerJudgment] == trueLabels[this.WorkerJudgment]).Named("labelsEqual");
                        Variable.ConstrainEqualRandom(labelsEqual, new Bernoulli(0.9999));  // Add a slight amount of noise due to Infer.NET compiler bug.
                    }

                    using (Variable.IfNot(workerIsCorrect))
                    {
                        this.WorkerLabel[this.Workers][this.WorkerJudgment] = Variable.Discrete(this.RandomGuessProbability);
                    }
                }
            }

            if (withEvidence)
            {
                block.CloseBlock();
            }

            this.HasEvidence = withEvidence;
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
            if (newWorkerToOldWorkerMap == null || oldPosteriors == null)
            {
                this.SetDefaultPriors();
            }
            else
            {
                this.SetPriorsFromPosteriors(newWorkerToOldWorkerMap, newWordToOldWordMap, oldPosteriors);
            }

            var posteriors = new HonestWorkerModelPosteriors();
            var evidences = new List<double>();
            for (var it = 1; it <= numIterations; it++)
            {
                this.Engine.NumberOfIterations = it;
                posteriors.TrueLabel = this.Engine.Infer<Discrete[]>(this.TrueLabel);
                posteriors.BackgroundLabelProb = this.Engine.Infer<Dirichlet>(this.ProbLabel);
                posteriors.RandomGuessProbability = this.Engine.Infer<Dirichlet>(this.RandomGuessProbability);
                posteriors.WorkerAbility = this.Engine.Infer<Beta[]>(this.Ability);
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

            return posteriors;
        }

        /// <inheritdoc />
        public override void SetDefaultPriors()
        {
            base.SetDefaultPriors();
            this.RandomGuessPrior.ObservedValue = Dirichlet.Uniform(this.Labels.SizeAsInt);
            this.AbilityPrior.ObservedValue = Util.ArrayInit(this.WorkerCount, input => new Beta(2, 1));
        }

        /// <inheritdoc />
        public override void SetPriorsFromPosteriors(
            int[] newWorkerToOldWorkerMap,
            int[] newWordToOldWordMap,
            ModelPosteriors modelPosteriors)
        {
            base.SetPriorsFromPosteriors(newWorkerToOldWorkerMap, newWordToOldWordMap, modelPosteriors);
            var honestWorkerModelPosteriors = (HonestWorkerModelPosteriors)modelPosteriors;
            this.RandomGuessPrior.ObservedValue = honestWorkerModelPosteriors.RandomGuessProbability;
            this.AbilityPrior.ObservedValue = Util.ArrayInit(this.WorkerCount, input => new Beta(2, 1));
            for (var i = 0; i < newWorkerToOldWorkerMap.Length; i++)
            {
                var oldIdx = newWorkerToOldWorkerMap[i];
                if (oldIdx >= 0)
                {
                    this.AbilityPrior.ObservedValue[i] = honestWorkerModelPosteriors.WorkerAbility[oldIdx];
                }
            }
        }

        /// <summary>
        /// The honest worker model posteriors class.
        /// </summary>
        [Serializable]
        public class HonestWorkerModelPosteriors : ModelPosteriors
        {
            /// <summary>
            /// Gets the inferred random guess probability.
            /// </summary>
            public Dirichlet RandomGuessProbability { get; internal set; }

            /// <summary>
            /// Gets the inferred worker abilities.
            /// </summary>
            public Beta[] WorkerAbility { get; internal set; }
        }
    }
}
