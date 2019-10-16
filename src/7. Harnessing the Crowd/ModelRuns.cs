// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ML.Probabilistic.Distributions;
using Microsoft.ML.Probabilistic.Math;
using Microsoft.ML.Probabilistic.Utilities;
using static HarnessingTheCrowd.RunnerBase;
#if NETFULL
using Point = System.Windows.Point;
#else
using Point = MBMLCommon.Point;
#endif

namespace HarnessingTheCrowd
{
    /// <summary>
    /// Static methods for running the inference for the various scenarios in the chapter
    /// </summary>
    public static class ModelRuns
    {
        /// <summary>
        ///     The training key in the results.
        /// </summary>
        public const string TrainingKey = "Training";

        /// <summary>
        ///     The validation key in the results.
        /// </summary>
        public const string ValidationKey = "Validation";

        /// <summary>
        ///     The validation key in the results.
        /// </summary>
        public const string ValidationNoLabelsKey = "ValidationNoLabels";

        /// <summary>
        ///     The validation key in the results.
        /// </summary>
        public const string ErrorsKey = "Errors";

        /// <summary>
        ///     The worker metrics key in the results.
        /// </summary>
        public const string WorkerMetricsKey = "Worker Metrics";

        /// <summary>
        ///     The label count histogram key in the results.
        /// </summary>
        public const string LabelCountHistogramKey = "Label Count Histogram";

        /// <summary>
        ///     The label values in the dataset, and their string representations.
        /// </summary>
        public static Dictionary<int, string> LabelValuesToString =>
            new Dictionary<int, string>
            {
                {0, "Negative"},
                {1, "Neutral"},
                {2, "Positive"},
                {3, "Unrelated"}
            };

        /// <summary>
        ///     Runs a model sweep and returns Glo-ready results.
        /// </summary>
        /// <param name="trainingDataSets">
        ///     Training data sets.
        /// </param>
        /// <param name="validationDataSet">
        ///     Validation data set
        /// </param>
        /// <param name="model">
        ///     The model.
        /// </param>
        /// <param name="corpusInformation">
        ///     Corpus information.
        /// </param>
        /// <param name="runnerCreator">
        ///     A method to create a runner.
        /// </param>
        /// <param name="trainingResultGetter">
        ///     A method that gets the training results in a form for consumption by Glo.
        /// </param>
        /// <param name="validationResultGetter">
        ///     The validation result getter.
        /// </param>
        /// <param name="experimentParameters">
        ///     The experiment parameters.
        /// </param>
        /// <param name="validationDataSetNoLabels">
        ///     Validation data set with no labels
        /// </param>
        /// <param name="previousTrainingRunners">
        ///     Training runners from a previous run.
        /// </param>
        /// <param name="numIterations">
        ///     The number of iterations.
        /// </param>
        /// <param name="resultStorage">
        ///     Optional externally created dictionary to store the results of the current sweep. Can be provided e.g. to show the results as they arrive.
        /// </param>
        /// <returns>
        ///     The metrics for the validation sets, appended by the metrics for the training set.
        /// </returns>
        public static (Dictionary<string, Dictionary<string, object>> Results, List<ModelRunnerBase> TrainingRunners)
            RunSweep(
                CrowdDataWithText[] trainingDataSets,
                CrowdDataWithText validationDataSet,
                ModelBase model,
                CorpusInformation corpusInformation,
                Func<CrowdDataMapping, ModelBase, ModelRunnerBase, ModelRunnerBase> runnerCreator,
                Func<ModelRunnerBase, Dictionary<string, object>> trainingResultGetter,
                Func<ModelRunnerBase, Dictionary<string, object>, Dictionary<string, object>> validationResultGetter,
                ExperimentParameters experimentParameters,
                CrowdDataWithText validationDataSetNoLabels = null,
                List<ModelRunnerBase> previousTrainingRunners = null,
                int numIterations = 200,
                Dictionary<string, Dictionary<string, object>> resultStorage = null)
        {
            var allResultsForThisModel = resultStorage ?? new Dictionary<string, Dictionary<string, object>>();
            var trainingRunners = new List<ModelRunnerBase>();
            const int NumIterationsForValidation = 5;

            for (var i = 0; i < trainingDataSets.Length; i++)
            {
                var currentModelName =
                    $"{model.Name}_{i}"; // Chart code assumes suffix of dash then number for ordering chart points
                var trainingData = trainingDataSets[i];
                var trainingMapping = new CrowdDataWithTextMapping(
                    trainingData,
                    LabelValuesToString,
                    corpusInformation);

                var trainingRunner = runnerCreator.Invoke(trainingMapping, model, previousTrainingRunners?[i]);
                trainingRunners.Add(trainingRunner);
                Rand.Restart(experimentParameters.RandomSeed);
                RunModel(trainingRunner, currentModelName + "_Training", numIterations,
                    experimentParameters.UseGoldLabelsInTraining);
                var trainingResults = trainingResultGetter?.Invoke(trainingRunner) ?? new Dictionary<string, object>();

                trainingResults[ErrorsKey] = trainingRunner.GetErrors();
                trainingResults[WorkerMetricsKey] = GetWorkerMetrics(trainingRunner.DataMapping.Data, trainingRunner, experimentParameters.MaximumNumberWorkers);

                var currentResults =
                    new Dictionary<string, object> { [TrainingKey] = trainingResults };

                var validationMapping = new CrowdDataWithTextMapping(
                    validationDataSet,
                    LabelValuesToString,
                    corpusInformation);

                var validationRunner = runnerCreator.Invoke(validationMapping, model, trainingRunner);
                Rand.Restart(experimentParameters.RandomSeed);
                var validationMetrics = RunModel(validationRunner, currentModelName + "_Validation",
                    NumIterationsForValidation, false);

                foreach (var prediction in validationRunner.Posteriors.TrueLabel) Console.WriteLine(prediction);

                var validationResults = validationResultGetter.Invoke(validationRunner, validationMetrics);
                validationResults[ErrorsKey] = validationRunner.GetErrors();
                validationResults[WorkerMetricsKey] = GetWorkerMetrics(validationRunner.DataMapping.Data,
                    validationRunner, experimentParameters.MaximumNumberWorkers);
                currentResults[ValidationKey] = validationResults;

                if (validationDataSetNoLabels != null)
                {
                    var validationMappingNoLabels = new CrowdDataWithTextMapping(
                        validationDataSetNoLabels,
                        LabelValuesToString,
                        corpusInformation);

                    var validationNoLabelsRunner =
                        runnerCreator.Invoke(validationMappingNoLabels, model, trainingRunner);
                    Rand.Restart(experimentParameters.RandomSeed);
                    var validationMetricsNoLabels = RunModel(validationNoLabelsRunner,
                        currentModelName + "_ValidationNoLabels", NumIterationsForValidation, false);
                    currentResults[ValidationNoLabelsKey] =
                        validationResultGetter.Invoke(validationNoLabelsRunner, validationMetricsNoLabels);
                }

                allResultsForThisModel[$"TrainingPercent_{Math.Min((i + 1) * 100 / trainingDataSets.Length, 100)}"] =
                    currentResults;
            }

            return (allResultsForThisModel, trainingRunners);
        }

        /// <summary>
        ///     Run the majority vote model.
        /// </summary>
        /// <param name="validationDataSet">
        ///     The validation data set.
        /// </param>
        /// <param name="modelName">
        ///     The model name.
        /// </param>
        /// <returns>
        ///     The metrics for the validation set.
        /// </returns>
        public static Dictionary<string, object> RunMajorityVoteModel(
            CrowdDataWithText validationDataSet,
            string modelName)
        {
            var validationRunner = (RunnerBase)new MajorityVoteRunner(
                new CrowdDataMapping(validationDataSet, LabelValuesToString));

            var result = RunModel(validationRunner, modelName);
            return result;
        }

        /// <summary>
        ///     Run the random model.
        /// </summary>
        /// <param name="validationDataSet">
        ///     The validation data set.
        /// </param>
        /// <param name="modelName">
        ///     The model name.
        /// </param>
        /// <returns>
        ///     The metrics for the validation set.
        /// </returns>
        public static Dictionary<string, object> RunRandomModel(
            CrowdDataWithText validationDataSet,
            string modelName)
        {
            var validationRunner = (RunnerBase)new RandomModelRunner(
                new CrowdDataMapping(validationDataSet, LabelValuesToString));

            var result = RunModel(validationRunner, $"{modelName}Validation");
            return result;
        }

        /// <summary>
        ///     Runs a model.
        /// </summary>
        /// <param name="runner">
        ///     The runner.
        /// </param>
        /// <param name="modelName">
        ///     The results file name.
        /// </param>
        /// <param name="numIterations">
        ///     The number of iterations.
        /// </param>
        /// <param name="useGoldLabels">Use gold labels in training.</param>
        /// <returns>
        ///     The dictionary of metrics.
        /// </returns>
        public static Dictionary<string, object> RunModel(RunnerBase runner, string modelName, int numIterations = 20,
            bool useGoldLabels = false)
        {
            Console.WriteLine($@"Running {modelName}");
            var metrics = runner.RunModel(numIterations, useGoldLabels);
            return metrics.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
        }

        /// <summary>
        ///     Gets the validation results.
        /// </summary>
        /// <param name="runner">
        ///     The runner.
        /// </param>
        /// <param name="validationMetrics">
        ///     The validation metrics.
        /// </param>
        /// <returns>
        ///     The validation results.
        /// </returns>
        public static Dictionary<string, object> GetValidationResults(ModelRunnerBase runner,
            Dictionary<string, object> validationMetrics)
        {
            var results = new Dictionary<string, object>();
            foreach (var kvp in validationMetrics)
            {
                var key = kvp.Key;
                var value = kvp.Value;
                if (kvp.Key == Metric.ConfusionMatrix.ToString())
                {
                    value = PlotData.GetConfusionMatrix((double[,])kvp.Value, runner.DataMapping,
                        PlotData.ConfusionMatrixRowLabel, PlotData.ConfusionMatrixColLabel2);
                    var valuePerc = PlotData.GetConfusionMatrix((double[,])kvp.Value, runner.DataMapping,
                        PlotData.ConfusionMatrixRowLabel, PlotData.ConfusionMatrixColLabel2, true);
                    results[key + "Percentage"] = valuePerc;
                }

                results[key] = value;
            }

            return results;
        }

        /// <summary>
        ///     Gets the validation results for the honest worker model.
        /// </summary>
        /// <param name="runner">
        ///     The runner.
        /// </param>
        /// <param name="validationMetrics">
        ///     The validation metrics.
        /// </param>
        /// <param name="maxNumberWorkers">
        ///     The maximum number of workers for whom to return validation results.
        /// </param>
        /// <returns>
        ///     The validation results.
        /// </returns>
        public static Dictionary<string, object> GetHonestWorkerValidationResults(ModelRunnerBase runner,
            Dictionary<string, object> validationMetrics, int maxNumberWorkers)
        {
            var results = GetValidationResults(runner, validationMetrics);

            // Scatter plot for ability versus accuracy
            var workerMetrics = GetWorkerMetrics(runner.DataMapping.Data, null, maxNumberWorkers);
            var accuracies = workerMetrics[Metric.Accuracy.ToString()];
            var abilities = ((HonestWorkerRunner)runner.TrainingRunner).WorkerAbility;
            var validationWorkersWithAbilities =
                accuracies.Keys.Where(wid => abilities.ContainsKey(wid)).ToList();
            var scatterPlot = validationWorkersWithAbilities
                .Select(wid => new Point((double)accuracies[wid], abilities[wid].GetMean())).ToList();
            results["Accuracy Ability"] = scatterPlot;
            return results;
        }

        /// <summary>
        ///     Gets <see cref="RunnerBase" /> results.
        /// </summary>
        /// <param name="runner">
        ///     The runner.
        /// </param>
        /// <returns>
        ///     The dictionary of results.
        /// </returns>
        public static Dictionary<string, object> GetTrainingResults(RunnerBase runner)
        {
            var result = new Dictionary<string, object>();
            var confusionMatrix = PlotData.GetConfusionMatrix(runner.ConfusionMatrix, runner.DataMapping,
                PlotData.ConfusionMatrixRowLabel, PlotData.ConfusionMatrixColLabel2);
            result["ConfusionMatrix"] = confusionMatrix;
            var confusionMatrixPercentage = PlotData.GetConfusionMatrix(runner.ConfusionMatrix, runner.DataMapping,
                PlotData.ConfusionMatrixRowLabel, PlotData.ConfusionMatrixColLabel2, true);
            result["ConfusionMatrixPercentage"] = confusionMatrixPercentage;
            var tweetMatrix = PlotData.GetTweetMatrix(runner.TweetMatrix, runner.DataMapping,
                PlotData.ConfusionMatrixRowLabel, "Inferred True");
            result["Accuracy"] = runner.Accuracy;
            result["AverageRecall"] = runner.AverageRecall;

            return result;
        }

        /// <summary>
        ///     Gets <see cref="ModelRunnerBase" /> results.
        /// </summary>
        /// <param name="runner">
        ///     The runner.
        /// </param>
        /// <returns>
        ///     The dictionary of results.
        /// </returns>
        private static Dictionary<string, object> GetModelTrainingResults(ModelRunnerBase runner)
        {
            var result = GetTrainingResults(runner);

            result["Evidence"] = runner.ModelEvidence;
            result["BackgroundLabelProb"] = runner.BackgroundLabelProb.PseudoCount.ToArray();
            result["Evidence"] = runner.AverageLogProbability;
            return result;
        }

        /// <summary>
        ///     Gets the <see cref="HonestWorkerRunner" /> results.
        /// </summary>
        /// <param name="runner">
        ///     The runner.
        /// </param>
        /// <returns>
        ///     The dictionary of results.
        /// </returns>
        public static Dictionary<string, object> GetHonestWorkerTrainingResults(ModelRunnerBase runner)
        {
            var result = GetModelTrainingResults(runner);
            var abilityHistogram = PlotData.GetWorkerAbilities((HonestWorkerRunner)runner);
            result["AbilityHistogram"] = abilityHistogram;

            return result;
        }

        /// <summary>
        ///     Gets the <see cref="BiasedWorkerModelRunner" /> results.
        /// </summary>
        /// <param name="runner">
        ///     The runner.
        /// </param>
        /// <param name="maximumNumberWorkers">
        ///     The maximum number of workers for whom to return results..
        /// </param>
        /// <returns>
        ///     The dictionary of results.
        /// </returns>
        public static Dictionary<string, object> GetBiasedWorkerTrainingResults(ModelRunnerBase runner,
            int maximumNumberWorkers = 20)
        {
            var result = GetModelTrainingResults(runner);
            if (runner is BiasedWorkerModelRunner biasedWorkerRunner)
            {
                var prominentWorkers = GetProminentWorkers(runner.DataMapping.Data, maximumNumberWorkers);

                var prominentWorkerCpts = biasedWorkerRunner.WorkerCpt
                    .Where(kvp => prominentWorkers.Contains(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                var workerCpts = PlotData.GetWorkerCpts(
                    prominentWorkerCpts,
                    runner.DataMapping);

                var workerCptsPerc = PlotData.GetWorkerCpts(
                    prominentWorkerCpts,
                    runner.DataMapping,
                    true);

                result["ProminentWorkerCpts"] = workerCpts;
                result["ProminentWorkerCpts"] = workerCptsPerc;
            }

            return result;
        }

        /// <summary>
        ///     Gets the <see cref="BiasedCommunityModelRunner" /> results.
        /// </summary>
        /// <param name="runner">
        ///     The runner.
        /// </param>
        /// <returns>
        ///     The dictionary of results.
        /// </returns>
        public static Dictionary<string, object> GetBiasedCommunityTrainingResults(ModelRunnerBase runner)
        {
            var result = GetModelTrainingResults(runner);

            if (runner is BiasedCommunityModelRunner biasedCommunityRunner)
            {
                var communityCpts = new Dictionary<string, Dirichlet[]>();
                for (var i = 0; i < biasedCommunityRunner.CommunityCpt.Count; i++)
                    communityCpts[i.ToString()] = biasedCommunityRunner.CommunityCpt[i];

                var plottableCommunityCpts = PlotData.GetWorkerCpts(
                    communityCpts,
                    runner.DataMapping);

                var communityCptsPerc = PlotData.GetWorkerCpts(
                    communityCpts,
                    runner.DataMapping,
                    true);

                result["CommunityCpts"] = plottableCommunityCpts;
                result["CommunityCptsPerc"] = communityCptsPerc;
                result["WorkerCommunities"] = biasedCommunityRunner.WorkerCommunities;
                result["WorkerCommunityCounts"] = biasedCommunityRunner.WorkerCommunityCounts;
            }

            return result;
        }

        /// <summary>
        ///     Gets the <see cref="BiasedCommunityWordsRunner" /> results.
        /// </summary>
        /// <param name="runner">
        ///     The runner.
        /// </param>
        /// <returns>
        ///     The dictionary of results.
        /// </returns>
        public static Dictionary<string, object> GetBiasedCommunityWordsRunnerResults(ModelRunnerBase runner)
        {
            var result = GetBiasedCommunityTrainingResults(runner);

            if (runner is BiasedCommunityWordsRunner biasedCommunityWordsRunner)
            {
                result["BackgroundLogProbWord"] = biasedCommunityWordsRunner.BackgroundLogProbWord;

                var logProbWord = biasedCommunityWordsRunner.LogProbWord.ToDictionary(
                    kvp => runner.DataMapping.LabelValueToString[kvp.Key],
                    kvp => kvp.Value);
                result["LogProbWord"] = logProbWord;

                var relativeLogProbWord = biasedCommunityWordsRunner.RelativeLogProbWord.ToDictionary(
                    kvp => runner.DataMapping.LabelValueToString[kvp.Key],
                    kvp => kvp.Value);
                result["RelativeLogProbWord"] = relativeLogProbWord;

                var orderedLogProbWord = biasedCommunityWordsRunner.OrderedLogProbWord.ToDictionary(
                    kvp => runner.DataMapping.LabelValueToString[kvp.Key],
                    kvp => kvp.Value);
                result["OrderedLogProbWord"] = orderedLogProbWord;

                var orderedRelativeLogProbWord = biasedCommunityWordsRunner.OrderedRelativeLogProbWord.ToDictionary(
                    kvp => runner.DataMapping.LabelValueToString[kvp.Key],
                    kvp => kvp.Value.ToDictionary(kvp1 => kvp1.Key, kvp1 => kvp1.Value));
                result["OrderedRelativeLogProbWord"] = orderedRelativeLogProbWord;

                result["TopTenWords"] = orderedRelativeLogProbWord.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.OrderByDescending(kvp1 => kvp1.Value).Take(10).Select(kvp1 => Regex.Replace(kvp1.Key, @"[\d-]", string.Empty)).ToList());
            }

            return result;
        }

        /// <summary>
        ///     Write the crowd data details to console.
        /// </summary>
        /// <param name="crowdData">
        ///     The crowd data.
        /// </param>
        /// <param name="dataName">
        ///     The data name.
        /// </param>
        public static void WriteCrowdDataDetailsToConsole(CrowdData crowdData, string dataName)
        {
            Console.WriteLine($@"{dataName}:");
            Console.WriteLine($@"  Number of tweets: {crowdData.NumTweets}");
            Console.WriteLine($@"  Number of tweets with gold labels: {crowdData.NumGoldTweets}");
            Console.WriteLine($@"  Number of workers: {crowdData.NumWorkers}");
            Console.WriteLine($@"  Number of worker labels: {crowdData.NumLabels}");
            Console.WriteLine();
        }

        /// <summary>
        ///     Gets the worker metrics for a data set
        /// </summary>
        /// <param name="data">
        ///     The data.
        /// </param>
        /// <param name="runner">
        ///     The runner.
        /// </param>
        /// <param name="maxNumberWorkers">
        ///     The maximum number of workers for which to get metrics.
        /// </param>
        /// <returns>
        ///     The metrics.
        /// </returns>
        public static Dictionary<string, Dictionary<string, object>> GetWorkerMetrics(CrowdData data,
            RunnerBase runner, int maxNumberWorkers = 20)
        {
            var metricsForWorkers = new List<Metric> { Metric.Count, Metric.Accuracy, Metric.ConfusionMatrix };
            var result = new Dictionary<string, Dictionary<string, object>>();
            var mapping = runner != null ? runner.DataMapping : new CrowdDataMapping(data, LabelValuesToString);

            foreach (var metric in metricsForWorkers) result[metric.ToString()] = new Dictionary<string, object>();

            var confusionMatrixKey = Metric.ConfusionMatrix.ToString();
            var confusionMatrixPercentageKey = $"{confusionMatrixKey}Percentage";
            result[confusionMatrixPercentageKey] = new Dictionary<string, object>();

            var labelsGroupedByWorker = data.CrowdLabels.GroupBy(cd => cd.WorkerId);
            var trueLabels = runner?.Predictions;
            const string RowLabel = PlotData.ConfusionMatrixRowLabel;
            const string ColumnLabel = PlotData.ConfusionMatrixColLabel;

            foreach (var worker in labelsGroupedByWorker)
            {
                var workerId = worker.Key;
                var workerLabels = worker.Distinct(CrowdData.WorkerTweetEqualityComparer.Instance)
                    .ToDictionary(lab => lab.TweetId, lab => lab.WorkerLabel);
                var workerMetrics = GetMetrics(mapping, workerLabels, trueLabels);

                foreach (var metric in metricsForWorkers)
                    if (metric == Metric.ConfusionMatrix)
                    {
                        var mat = workerMetrics[metric];
                        var confMat = PlotData.GetConfusionMatrix((double[,])mat, mapping, RowLabel, ColumnLabel);
                        result[confusionMatrixKey][workerId] = confMat;
                        var matPerc =
                            PlotData.GetConfusionMatrix((double[,])mat, mapping, RowLabel, ColumnLabel, true);
                        result[confusionMatrixPercentageKey][workerId] = matPerc;
                    }
                    else
                    {
                        result[metric.ToString()][workerId] = workerMetrics[metric];
                    }
            }

            // Limit the confusion matrices to the more prominent workers.
            var prominentWorkers = new HashSet<string>(result[Metric.Count.ToString()]
                .OrderByDescending(kvp => (int)kvp.Value).Take(maxNumberWorkers).Select(kvp => kvp.Key));
            result[confusionMatrixKey] = result[confusionMatrixKey].Where(kvp1 => prominentWorkers.Contains(kvp1.Key))
                .ToDictionary(kvp1 => kvp1.Key, kvp1 => kvp1.Value);
            result[confusionMatrixPercentageKey] = result[confusionMatrixPercentageKey]
                .Where(kvp1 => prominentWorkers.Contains(kvp1.Key))
                .ToDictionary(kvp1 => kvp1.Key, kvp1 => kvp1.Value);

            return result;
        }

        /// <summary>
        ///     Get5s the worker label count histogram.
        /// </summary>
        /// <param name="data">
        ///     The data.
        /// </param>
        /// <returns>
        ///     The histogram.
        /// </returns>
        public static int?[] GetWorkerLabelCountHistogram(CrowdData data)
        {
            var labelCounts = data.CrowdLabels.GroupBy(datum => datum.WorkerId).Select(grp => grp.Count()).ToList();
            var maxCount = labelCounts.Count == 0 ? 0 : labelCounts.Max();
            var result = Util.ArrayInit<int?>(maxCount + 1, i => 0);

            foreach (var cnt in labelCounts) result[cnt]++;

            result[0] = null;

            return result;
        }

        /// <summary>
        ///     Gets the prominent workers for a data set - i.e. those who have given many labels.
        /// </summary>
        /// <param name="data">
        ///     The data.
        /// </param>
        /// <param name="maxNumberWorkers">
        ///     The maximum number of workers for which to get metrics.
        /// </param>
        /// <returns>
        ///     The metrics.
        /// </returns>
        private static HashSet<string> GetProminentWorkers(CrowdData data, int maxNumberWorkers = 20)
        {
            var mapping = new CrowdDataMapping(
                data,
                LabelValuesToString);

            var labelCounts = new Dictionary<string, object>();

            var labelsGroupedByWorker = data.CrowdLabels.GroupBy(cd => cd.WorkerId);

            foreach (var worker in labelsGroupedByWorker)
            {
                var workerId = worker.Key;
                var workerLabels = worker.Distinct(CrowdData.WorkerTweetEqualityComparer.Instance)
                    .ToDictionary(lab => lab.TweetId, lab => lab.WorkerLabel);
                var workerMetrics = GetMetrics(mapping, workerLabels);
                labelCounts[workerId] = workerMetrics[Metric.Count];
            }

            return new HashSet<string>(
                labelCounts.OrderByDescending(kvp => (int)kvp.Value).Take(maxNumberWorkers).Select(kvp => kvp.Key));
        }
    }
}
