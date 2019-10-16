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
    /// The biased worker model class.
    /// </summary>
    public class BiasedWorkerModel : ModelBase
    {
        public BiasedWorkerModel(InferenceEngine engine) : base(engine)
        {
        }

        /// <summary>
        /// Gets or sets the initial on-diagonal pseudo-count.
        /// </summary>
        public static double InitialOnDiagonalPseudoCount { get; set; } = 60.0;

        /// <summary>
        /// Gets or sets the initial off-diagonal pseudo-count.
        /// </summary>
        public static double InitialOffDiagonalPseudoCount { get; set; } = 10.0;

        /// <inheritdoc />
        public override string Name => "Biased worker";

        /// <summary>
        /// Gets or sets the worker conditional probability table.
        /// </summary>
        protected VariableArray<VariableArray<Vector>, Vector[][]> ProbWorkerLabel { get; set; }

        /// <summary>
        /// Gets or sets the conditional probability table prior distribution variable.
        /// </summary>
        protected VariableArray<VariableArray<Dirichlet>, Dirichlet[][]> ProbWorkerLabelPrior { get; set; }

        /// <summary>
        /// Returns a conditional probability table prior.
        /// </summary>
        /// <param name="onDiagonalPseudoCount">
        /// The on Diagonal Pseudo Count.
        /// </param>
        /// <param name="offDiagonalPseudoCount">
        /// The off Diagonal Pseudo Count.
        /// </param>
        /// <param name="labelCount">
        /// The label Count.
        /// </param>
        /// <returns>
        /// The conditional probability table of each worker.
        /// </returns>
        public static Dirichlet[] GetCptPrior(double onDiagonalPseudoCount, double offDiagonalPseudoCount, int labelCount)
        {
            var cptPrior = new Dirichlet[labelCount];
            for (var d = 0; d < labelCount; d++)
            {
                cptPrior[d] = new Dirichlet(Util.ArrayInit(labelCount, i => i == d ? onDiagonalPseudoCount : offDiagonalPseudoCount));
            }

            return cptPrior;
        }

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

            // Worker biases
            this.ProbWorkerLabelPrior = Variable.Array(Variable.Array<Dirichlet>(this.Labels), this.Workers).Named("probWorkerLabelPrior");
            this.ProbWorkerLabel = Variable.Array(Variable.Array<Vector>(this.Labels), this.Workers).Named("probWorkerLabel");
            this.ProbWorkerLabel[this.Workers][this.Labels] = Variable<Vector>.Random(this.ProbWorkerLabelPrior[this.Workers][this.Labels]);
            this.ProbWorkerLabel.SetValueRange(this.Labels);

            // Condition on latent truth
            using (Variable.ForEach(this.Workers))
            {
                var trueLabels = Variable.Subarray(this.TrueLabel, this.WorkerJudgedTweetIndex[this.Workers]).Named("trueLabelSubarray");
                trueLabels.SetValueRange(this.Labels);
                using (Variable.ForEach(this.WorkerJudgment))
                {
                    var trueLabel = trueLabels[this.WorkerJudgment];
                    using (Variable.Switch(trueLabel))
                    {
                        this.WorkerLabel[this.Workers][this.WorkerJudgment] = Variable.Discrete(this.ProbWorkerLabel[this.Workers][trueLabel]);
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
        public override void SetDefaultPriors()
        {
            base.SetDefaultPriors();
            this.ProbWorkerLabelPrior.ObservedValue = Util.ArrayInit(this.WorkerCount, input => GetCptPrior(InitialOnDiagonalPseudoCount, InitialOffDiagonalPseudoCount, this.LabelValueCount));
        }

        /// <inheritdoc />
        public override void SetPriorsFromPosteriors(
            int[] newWorkerToOldWorkerMap,
            int[] newWordToOldWordMap,
            ModelPosteriors modelPosteriors)
        {
            base.SetPriorsFromPosteriors(newWorkerToOldWorkerMap, newWordToOldWordMap, modelPosteriors);
            var biasedWorkerModelPosteriors = (BiasedWorkerModelPosteriors)modelPosteriors;
            this.ProbWorkerLabelPrior.ObservedValue = Util.ArrayInit(this.WorkerCount, input => GetCptPrior(InitialOnDiagonalPseudoCount, InitialOffDiagonalPseudoCount, this.LabelValueCount));
            for (var i = 0; i < newWorkerToOldWorkerMap.Length; i++)
            {
                var oldIdx = newWorkerToOldWorkerMap[i];
                if (oldIdx >= 0)
                {
                    this.ProbWorkerLabelPrior.ObservedValue[i] = biasedWorkerModelPosteriors.WorkerCpt[oldIdx];
                }
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
            if (newWorkerToOldWorkerMap == null || oldPosteriors == null)
            {
                this.SetDefaultPriors();
            }
            else
            {
                this.SetPriorsFromPosteriors(newWorkerToOldWorkerMap, newWordToOldWordMap, oldPosteriors);
            }

            var posteriors = new BiasedWorkerModelPosteriors();
            var evidences = new List<double>();
            for (var it = 1; it <= numIterations; it++)
            {
                this.Engine.NumberOfIterations = it;
                posteriors.TrueLabel = this.Engine.Infer<Discrete[]>(this.TrueLabel);
                posteriors.WorkerCpt = this.Engine.Infer<Dirichlet[][]>(this.ProbWorkerLabel);
                posteriors.BackgroundLabelProb = this.Engine.Infer<Dirichlet>(this.ProbLabel);
                if (this.HasEvidence)
                {
                    posteriors.Evidence = this.Engine.Infer<Bernoulli>(this.Evidence);
                    Console.WriteLine($"Iteration {it} log evidence:\t{posteriors.Evidence.LogOdds:0.00}");
                    evidences.Add(posteriors.Evidence.LogOdds);
                    if (ModelBase.HasConverged(evidences))
                    {
                        break;
                    }
                }
            }

            return posteriors;
        }

        /// <summary>
        /// The biased worker model posteriors class.
        /// </summary>
        [Serializable]
        public class BiasedWorkerModelPosteriors : ModelPosteriors
        {
            /// <summary>
            /// Gets the Dirichlet parameters of the conditional probability table of each worker.
            /// </summary>
            public Dirichlet[][] WorkerCpt { get; internal set; }
        }
    }
}
