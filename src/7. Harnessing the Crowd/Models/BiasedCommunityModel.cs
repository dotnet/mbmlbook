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
    /// The biased community model.
    /// </summary>
    public class BiasedCommunityModel : ModelBase
    {
        public BiasedCommunityModel(InferenceEngine engine) : base(engine)
        {
        }

        /// <summary>
        /// Gets or sets the number of communities.
        /// </summary>
        public int NumberOfCommunities
        {
            get => this.NumCommunities.IsObserved ? this.NumCommunities.ObservedValue : 0;
            set => this.NumCommunities.ObservedValue = value;
        }

        /// <inheritdoc />
        public override string Name => $"Community ({this.NumberOfCommunities})";

        /// <summary>
        /// Gets or sets the range for the number of communities.
        /// </summary>
        protected Range Communities { get; set; }

        /// <summary>
        /// Gets or sets the worker community.
        /// </summary>
        protected VariableArray<int> Community { get; set; }

        /// <summary>
        /// Gets or sets the worker community.
        /// </summary>
        protected VariableArray<Discrete> ProbCommunity { get; set; }

        /// <summary>
        /// Gets or sets the worker community initializer.
        /// </summary>
        protected Variable<IDistribution<int[]>> WorkerCommunityInitializer { get; set; }

        /// <summary>
        /// Gets or sets the worker conditional probability table.
        /// </summary>
        protected VariableArray<VariableArray<Vector>, Vector[][]> ProbWorkerLabel { get; set; }

        /// <summary>
        /// Gets or sets the conditional probability table prior distribution variable.
        /// </summary>
        protected VariableArray<VariableArray<Dirichlet>, Dirichlet[][]> ProbWorkerLabelPrior { get; set; }

        /// <summary>
        /// Gets or sets the number of communities.
        /// </summary>
        protected Variable<int> NumCommunities { get; set; } = Variable.New<int>().Named("numCommunities");

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
            this.CreateModelLikelihood();

            if (withEvidence)
            {
                block.CloseBlock();
            }

            this.HasEvidence = withEvidence;
        }

        /// <inheritdoc />
        public override void CreateModelStub(int numTweets, int numClasses, bool withGoldLabels = false)
        {
            base.CreateModelStub(numTweets, numClasses, withGoldLabels);
            this.Communities = new Range(this.NumCommunities).Named("communities");

            // Community biases
            this.ProbWorkerLabelPrior = Variable.Array(Variable.Array<Dirichlet>(this.Labels), this.Communities).Named("probWorkerLabelPrior");
            this.ProbWorkerLabel = Variable.Array(Variable.Array<Vector>(this.Labels), this.Communities).Named("probWorkerLabel");
            this.ProbWorkerLabel[this.Communities][this.Labels] = Variable<Vector>.Random(this.ProbWorkerLabelPrior[this.Communities][this.Labels]);
            this.ProbWorkerLabel.SetValueRange(this.Labels);

            // Pick the worker community
            this.ProbCommunity = Variable.Array<Discrete>(this.Workers).Named("probCommunity");
            this.Community = Variable.Array<int>(this.Workers).Named("community");
            this.Community[this.Workers] = Variable<int>.Random(this.ProbCommunity[this.Workers]);
            this.Community.SetValueRange(this.Communities);

            // Symmetry breaking
            this.WorkerCommunityInitializer = Variable.New<IDistribution<int[]>>().Named("workerCommunityInitializer");
            this.Community.InitialiseTo(this.WorkerCommunityInitializer);
        }

        /// <summary>
        /// Creates the model likelihood.
        /// </summary>
        public virtual void CreateModelLikelihood()
        {
            // Condition on latent truth
            using (Variable.ForEach(this.Workers))
            {
                var trueLabels = Variable.Subarray(this.TrueLabel, this.WorkerJudgedTweetIndex[this.Workers]).Named("trueLabelSubarray");
                trueLabels.SetValueRange(this.Labels);
                var community = this.Community[this.Workers];

                using (Variable.Switch(community))
                {
                    using (Variable.ForEach(this.WorkerJudgment))
                    {
                        var trueLabel = trueLabels[this.WorkerJudgment];
                        using (Variable.Switch(trueLabels[this.WorkerJudgment]))
                        {
                            this.WorkerLabel[this.Workers][this.WorkerJudgment] = Variable.Discrete(
                                this.ProbWorkerLabel[community][trueLabel]);
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        public override void SetDefaultPriors()
        {
            base.SetDefaultPriors();
            this.ProbWorkerLabelPrior.ObservedValue = Util.ArrayInit(this.NumberOfCommunities, input => BiasedWorkerModel.GetCptPrior(BiasedWorkerModel.InitialOnDiagonalPseudoCount, BiasedWorkerModel.InitialOffDiagonalPseudoCount, this.LabelValueCount));
            this.ProbCommunity.ObservedValue = Util.ArrayInit(this.WorkerCount, w => Discrete.Uniform(this.NumberOfCommunities));
        }

        /// <inheritdoc />
        public override void SetPriorsFromPosteriors(
            int[] newWorkerToOldWorkerMap,
            int[] newWordToOldWordMap,
            ModelPosteriors modelPosteriors)
        {
            base.SetPriorsFromPosteriors(newWorkerToOldWorkerMap, newWordToOldWordMap, modelPosteriors);
            var biasedCommunityModelPosteriors = (BiasedCommunityModelPosteriors)modelPosteriors;
            this.ProbWorkerLabelPrior.ObservedValue = biasedCommunityModelPosteriors.CommunityCpt;
            this.ProbCommunity.ObservedValue = Util.ArrayInit(this.WorkerCount, w => Discrete.Uniform(this.NumberOfCommunities));
            if (newWorkerToOldWorkerMap != null)
            {
                for (var i = 0; i < newWorkerToOldWorkerMap.Length; i++)
                {
                    var oldIdx = newWorkerToOldWorkerMap[i];
                    if (oldIdx >= 0)
                    {
                        this.ProbCommunity.ObservedValue[i] =
                            biasedCommunityModelPosteriors.WorkerCommunities[oldIdx];
                    }
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

            // Initialize messages
            var discreteUniform = Discrete.Uniform(this.NumberOfCommunities);
            this.WorkerCommunityInitializer.ObservedValue = Distribution<int>.Array(Util.ArrayInit(workerLabel.Length, w => Discrete.PointMass(discreteUniform.Sample(), this.NumberOfCommunities)));

            var posteriors = new BiasedCommunityModelPosteriors();
            var evidences = new List<double>();
            for (var it = 1; it <= numIterations; it++)
            {
                this.Engine.NumberOfIterations = it;
                posteriors.TrueLabel = this.Engine.Infer<Discrete[]>(this.TrueLabel);
                posteriors.CommunityCpt = this.Engine.Infer<Dirichlet[][]>(this.ProbWorkerLabel);
                posteriors.WorkerCommunities = this.Engine.Infer<Discrete[]>(this.Community);
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
        /// The biased community model posteriors class.
        /// </summary>
        [Serializable]
        public class BiasedCommunityModelPosteriors : ModelPosteriors
        {
            /// <summary>
            /// Gets the Dirichlet parameters of the conditional probability table of each worker.
            /// </summary>
            public Dirichlet[][] CommunityCpt { get; set; }

            /// <summary>
            /// Gets the community of each worker.
            /// </summary>
            public Discrete[] WorkerCommunities { get; set; }
        }
    }
}
