// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;

    using UnclutteringYourInbox.Features;
    using UnclutteringYourInbox.Models;
#if NETFULL
    using Point = System.Windows.Point;
#else
    using Point = MBMLCommon.Point;
#endif

    /// <summary>
    /// The mode.
    /// </summary>
    public enum ExperimentMode
    {
        /// <summary>
        /// offline mode (use full training set)
        /// </summary>
        Offline,

        /// <summary>
        /// incremental mode (training on increasing training set sizes).
        /// </summary>
        Incremental,

        /// <summary>
        /// online mode (Gaussian density filtering).
        /// </summary>
        Online,

        /// <summary>
        /// Community mode. Train a model on a set of users.
        /// </summary>
        Community
    }

    /// <summary>
    /// The experiment.
    /// </summary>
    public class Experiment
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Experiment"/> class.
        /// </summary>
        public Experiment()
        {
            this.Metrics = new List<Metrics>();
            this.CommunityResults = new List<Results>();
            this.Results = new List<Results>();
            this.TrainTimings = new List<TimeSpan>();
            this.TestTimings = new List<TimeSpan>();
        }

        /// <summary>
        /// Gets or sets the count.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the model name.
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// Gets or sets the user name.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the feature set.
        /// </summary>
        public string FeatureSetName { get; set; }

        /// <summary>
        /// Gets or sets the train timings.
        /// </summary>
        public List<TimeSpan> TrainTimings { get; set; }

        /// <summary>
        /// Gets or sets the test timings.
        /// </summary>
        public List<TimeSpan> TestTimings { get; set; }

        /// <summary>
        /// Gets the timing curve.
        /// </summary>
        public Point[] TrainTimingCurve
        {
            get
            {
                return this.TrainTimings.Select((ia, i) => new Point((i + 1) * this.BatchSize, ia.TotalSeconds)).ToArray();
            }
        }

        /// <summary>
        /// Gets the test timing curve.
        /// </summary>
        public Point[] TestTimingCurve
        {
            get
            {
                return this.TestTimings.Select((ia, i) => new Point((i + 1) * this.BatchSize, ia.TotalSeconds)).ToArray();
            }
        }

        /// <summary>
        /// Gets or sets the results.
        /// </summary>
        public List<Results> Results { get; set; }

        /// <summary>
        /// Gets or sets the community results.
        /// </summary>
        public List<Results> CommunityResults { get; set; }

        /// <summary>
        /// Gets or sets the metrics.
        /// </summary>
        public List<Metrics> Metrics { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether fully observed.
        /// </summary>
        public bool FullyObserved { get; set; }

        /// <summary>
        /// Gets or sets the batch size.
        /// </summary>
        public int BatchSize { get; set; }

        /// <summary>
        /// Gets or sets the mode.
        /// </summary>
        public ExperimentMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the posteriors.
        /// </summary>
        public Posteriors Posteriors { get; set; }

        /// <summary>
        /// Runs the experiment.
        /// </summary>
        /// <param name="trainModel">The train model.</param>
        /// <param name="testModel">The test model.</param>
        /// <param name="inputs">The inputs.</param>
        /// <param name="priors">The priors.</param>
        /// <param name="thresholdAndNoiseVariance">The threshold and noise variance.</param>
        /// <param name="limit">The limit.</param>
        /// <param name="trainMode">The train mode.</param>
        /// <param name="testMode">The test mode.</param>
        public void Run(
            ReplyToModelBase trainModel,
            ReplyToModelBase testModel,
            Inputs inputs,
            Priors priors,
            double thresholdAndNoiseVariance,
            int limit = -1,
            InputMode trainMode = InputMode.Training,
            InputMode testMode = InputMode.Validation | InputMode.Testing)
        {
            this.ModelName = trainModel.Name;
            this.FeatureSetName = inputs.FeatureSet.ToString();
            trainModel.ConstructModel();
            testModel.ConstructModel();

            int numTrain = trainMode == InputMode.Training ? inputs.Train.Count : inputs.TrainAndValidation.Count;
            if (limit > 0)
            {
                numTrain = Math.Min(numTrain, limit);
            }

            if (this.BatchSize == -1)
            {
                this.BatchSize = numTrain;
            }

            int numBatches = 1;
            if (this.Mode == ExperimentMode.Incremental || this.Mode == ExperimentMode.Online)
            {
                numBatches = numTrain / this.BatchSize;
            }

            for (int index = 0; index < numBatches; index++)
            {
                var batchResults = new Results();
                var batch = this.GetBatch(inputs, index, trainMode);

                if (numBatches > 1)
                {
                    Console.WriteLine(@"{0} batch {1}/{2}, batch size {3}", this.UserName, index + 1, numBatches, batch.Count);
                }

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                trainModel.SetObservedValues(batch, inputs.FeatureSet, InputMode.Training, priors);

                try
                {
                    trainModel.DoInference(inputs.FeatureSet, ref batchResults);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }

                stopwatch.Stop();
                this.TrainTimings.Add(stopwatch.Elapsed);

                // Save the last set of posteriors
                this.Posteriors = batchResults.Posteriors;

                // Set the priors to be the posteriors from the previous run
                var newPriors = Priors.FromPosteriors(this.Posteriors, inputs.FeatureSet, thresholdAndNoiseVariance);

                if (this.Mode == ExperimentMode.Online || this.Mode == ExperimentMode.Incremental)
                {
                    if (this.Mode == ExperimentMode.Online)
                    {
                        priors = newPriors;
                    }

                    // clear posteriors to save on memory
                    batchResults.Posteriors = null;
                }

                stopwatch.Restart();

                // Validation and Test only
                testModel.Apply(inputs, inputs.FeatureSet, newPriors, ref batchResults, testMode);

                stopwatch.Stop();

                this.TestTimings.Add(stopwatch.Elapsed);

                this.Results.Add(batchResults);
                this.Metrics.Add(new Metrics(inputs, batchResults, this.Mode));

                this.Count++;
            }
        }

        /// <summary>
        /// Runs the community experiment.
        /// </summary>
        /// <param name="trainModel">The train model.</param>
        /// <param name="seedInputs">The seed inputs.</param>
        /// <param name="communityFeatureSet">The community feature set.</param>
        /// <param name="priors">The priors.</param>
        /// <param name="thresholdAndNoiseVariance">The threshold and noise variance.</param>
        /// <returns>The <see cref="CommunityPosteriors" />.</returns>
        public CommunityPosteriors RunCommunityTraining(
            CommunityModelBase trainModel,
            IList<Inputs> seedInputs,
            FeatureSet communityFeatureSet,
            CommunityPriors priors,
            double thresholdAndNoiseVariance)
        {
            this.ModelName = trainModel.Name;

            // Assume all users have the same feature set type!
            this.FeatureSetName = communityFeatureSet.ToString();

            trainModel.ConstructModel();

            // First train the seeds in offline mode
            trainModel.SetObservedValues(
                seedInputs.Select(ia => ia.Train.Instances).ToList(),
                communityFeatureSet,
                InputMode.CommunityTraining,
                priors);

            var communityResults = new Results();

            // Now infer the community posteriors
            trainModel.DoInference(communityFeatureSet, seedInputs.Select(ia => ia.UserName), ref communityResults);

            this.CommunityResults.Add(communityResults);
            return communityResults.CommunityPosteriors;
        }

        /// <summary>
        /// Runs the personalisation.
        /// </summary>
        /// <param name="trainModel">The train model.</param>
        /// <param name="testModel">The test model.</param>
        /// <param name="inputs">The personalisation inputs.</param>
        /// <param name="priors">The priors.</param>
        /// <param name="precisionShape">The precision shape.</param>
        /// <param name="precisionScale">The precision scale.</param>
        /// <param name="thresholdAndNoiseVariance">The threshold and noise variance.</param>
        /// <param name="limit">The limit.</param>
        /// <param name="testMode">The test mode.</param>
        public void RunPersonalisation(
            CommunityModelBase trainModel,
            CommunityModelBase testModel,
            Inputs inputs,
            CommunityPriors priors,
            double precisionShape,
            double precisionScale,
            double thresholdAndNoiseVariance,
            int limit = -1,
            InputMode testMode = InputMode.Validation | InputMode.Testing)
        {
            RunPersonalisation(
                trainModel,
                testModel,
                inputs,
                priors,
                posteriors => CommunityPriors.FromPosteriors(
                    posteriors,
                    inputs.FeatureSet,
                    precisionShape,
                    precisionScale,
                    thresholdAndNoiseVariance,
                    new[] { inputs.UserName }),
                limit,
                testMode);
        }

        /// <summary>
        /// Runs the personalisation.
        /// </summary>
        /// <param name="trainModel">The train model.</param>
        /// <param name="testModel">The test model.</param>
        /// <param name="inputs">The inputs.</param>
        /// <param name="priors">The priors.</param>
        /// <param name="point">The point.</param>
        /// <param name="thresholdAndNoiseVariance">The threshold and noise variance.</param>
        /// <param name="limit">The limit.</param>
        /// <param name="testMode">The test mode.</param>
        public void RunPersonalisation(
            CommunityModelBase trainModel,
            CommunityModelBase testModel,
            Inputs inputs,
            CommunityPriors priors,
            double point,
            double thresholdAndNoiseVariance,
            int limit = -1,
            InputMode testMode = InputMode.Validation | InputMode.Testing)
        {
            RunPersonalisation(
                trainModel,
                testModel,
                inputs,
                priors,
                posteriors => CommunityPriors.FromPosteriors(
                    posteriors,
                    inputs.FeatureSet,
                    point,
                    thresholdAndNoiseVariance,
                    new[] { inputs.UserName }),
                limit,
                testMode);
        }

        /// <summary>
        /// Runs the personalisation.
        /// </summary>
        /// <param name="trainModel">The train model.</param>
        /// <param name="testModel">The test model.</param>
        /// <param name="inputs">The inputs.</param>
        /// <param name="priors">The priors.</param>
        /// <param name="point">The point.</param>
        /// <param name="thresholdAndNoiseVariance">The threshold and noise variance.</param>
        /// <param name="limit">The limit.</param>
        /// <param name="testMode">The test mode.</param>
        private void RunPersonalisation(
            CommunityModelBase trainModel,
            CommunityModelBase testModel,
            Inputs inputs,
            CommunityPriors priors,
            Func<CommunityPosteriors, CommunityPriors> priorsFromPosteriors,
            int limit = -1,
            InputMode testMode = InputMode.Validation | InputMode.Testing)
        {
            trainModel.ConstructModel();
            testModel.ConstructModel();

            int numTrain = inputs.Train.Count;
            if (limit > 0)
            {
                numTrain = Math.Min(numTrain, limit);
            }

            if (this.BatchSize == -1)
            {
                this.BatchSize = numTrain;
            }

            int numBatches = numTrain / this.BatchSize;

            for (int index = 0; index < numBatches; index++)
            {
                var batchResults = new Results();
                var batch = this.GetBatch(inputs, index, InputMode.Training);

                if (numBatches > 1)
                {
                    Console.WriteLine(@"{0} batch {1}/{2}, batch size {3}", this.UserName, index + 1, numBatches, batch.Count);
                }

                trainModel.SetObservedValues(new[] { batch }, inputs.FeatureSet, InputMode.Training, priors);

                try
                {
                    trainModel.DoInference(inputs.FeatureSet, new[] { inputs.UserName }, ref batchResults);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }

                // Set the priors to be the posteriors from the previous run
                priors = priorsFromPosteriors(batchResults.CommunityPosteriors);

                if (this.Mode == ExperimentMode.Online)
                {
                    // clear posteriors to save memory
                    batchResults.Posteriors = null;
                }

                // Validation and Test only
                testModel.Apply(inputs, inputs.FeatureSet, priors, ref batchResults, testMode);

                this.CommunityResults.Add(batchResults);

                this.Metrics.Add(new Metrics(inputs, batchResults, ExperimentMode.Online));

                this.Count++;
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.UserName + " " + this.FeatureSetName + " " + this.ModelName;
        }

        /// <summary>
        /// Gets the batch.
        /// </summary>
        /// <param name="inputs">The inputs.</param>
        /// <param name="index">The index.</param>
        /// <param name="trainMode">The train mode.</param>
        /// <returns>
        /// The instances for the current batch.
        /// </returns>
        private IList<Inputs.Instance> GetBatch(Inputs inputs, int index, InputMode trainMode)
        {
            IList<Inputs.Instance> instances;
            switch (trainMode)
            {
                case InputMode.Training:
                    instances = inputs.Train.Instances;
                    break;
                case InputMode.TrainAndValidation:
                    instances = inputs.TrainAndValidation.Instances;
                    break;
                default:
                    throw new InvalidOperationException("Can't train on validation or test data");
            }

            IList<Inputs.Instance> batch;
            switch (this.Mode)
            {
                case ExperimentMode.Incremental:
                    batch = instances.Take((index + 1) * this.BatchSize).ToList();
                    break;
                case ExperimentMode.Community:
                case ExperimentMode.Online:
                    batch = instances.Skip((index + 1) * this.BatchSize).Take(this.BatchSize).ToList();
                    break;
                case ExperimentMode.Offline:
                default:
                    batch = instances;
                    break;
            }

            return batch;
        }
    }
}
