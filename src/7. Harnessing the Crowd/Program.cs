// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.Probabilistic.Math;
using Microsoft.ML.Probabilistic.Utilities;
using Microsoft.Research.Glo.Views;

using MBMLCommon;
using Microsoft.ML.Probabilistic.Algorithms;
using Microsoft.ML.Probabilistic.Models;
using System.IO;
using static HarnessingTheCrowd.RunnerBase;
using static HarnessingTheCrowd.ModelRuns;

namespace HarnessingTheCrowd
{
    /// <summary>
    ///     Main program class for MBML 'Harnessing the Crowd' chapter.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Unfortunately, we were not able to ship the dataset for this chapter,
        /// so the code will not run.
        /// It is still the correct code that was used to produce the result for the chapter.
        /// 
        /// Defines the entry point of the application.
        /// The first argument, if present, sets the folder to save the output artifacts in.
        /// </summary>
        public static void Main(string[] args)
        {
            InitializeUI();
            Outputter outputter = Outputter.GetOutputter(Contents.ChapterName);
            // We expect the data in a folder near the repository.
            const string dataPath = @"../../../../../../HarnessingTheCrowdData";

            try
            {
                // Parameters of the experiment to run.
                // Modify if you want to run only some of the models, try different numbers of communities, etc.
                var experimentParameters = new ExperimentParameters
                {
                    ExperimentNamePrefix = string.Empty,
                    RandomSeed = 12347,
                    FractionGoldLabelsReservedForTraining = 0.3,
                    UseOnlyTweetsWithGoldLabels = false,
                    UseGoldLabelsInTraining = false,
                    NumberOfTrainingTweets = 10000,
                    NumDataSizes = 10,
                    NumberOfCommunitiesSweep = new[] { 1, 2, 3 },
                    VocabularyThreshold = 10,
                    ModelTypes = ModelTypes.MajorityVote
                                 | ModelTypes.HonestWorker
                                 | ModelTypes.BiasedWorker
                                 | ModelTypes.BiasedCommunity
                                 | ModelTypes.BiasedCommunityWords
                };
                
                RunExperiments(outputter, experimentParameters, dataPath);
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nAn unhandled exception was thrown:\n{e}");
            }
            finally
            {
                if (args.Length == 1)
                {
                    Console.WriteLine("\n\nSaving outputs...");
                    outputter.SaveOutputAsProducedFlattening(args[0]);
                    Console.WriteLine("Done saving.");
                }
            }
        }

        public static void RunExperiments(Outputter outputter, ExperimentParameters experimentParameters, string dataPath)
        {
            var engine = new InferenceEngine(new ExpectationPropagation())
            {
                ShowFactorGraph = false,
                ShowWarnings = true,
                ShowProgress = false
            };

            // Set engine flags
            engine.Compiler.WriteSourceFiles = true;
            engine.Compiler.UseParallelForLoops = false;

            string labelsFileName = Path.Combine(dataPath, "HarnessingTheCrowdLabels.tsv");
            string goldLabelsFileName = Path.Combine(dataPath, "HarnessingTheCrowdGoldLabels.tsv");
            string textsFileName = Path.Combine(dataPath, "HarnessingTheCrowdTexts.tsv");
            if (!File.Exists(labelsFileName)
                || !File.Exists(goldLabelsFileName)
                || !File.Exists(textsFileName))
            {
                throw new FileNotFoundException("Unfortunately, we were not able to ship the data necessary to run the code for this chapter.");
            }

            var crowdData = CrowdDataWithText.LoadData(
                labelsFileName,
                goldLabelsFileName,
                textsFileName,
                new HashSet<int>(LabelValuesToString.Keys));

            Rand.Restart(experimentParameters.RandomSeed);

            // Preparing the input data
            var split = crowdData.SplitData(experimentParameters.FractionGoldLabelsReservedForTraining);
            var fullTrainingData = split[CrowdData.Mode.Training];
            var fullValidationData = split[CrowdData.Mode.Validation];

            var trainingData = (CrowdDataWithText)fullTrainingData.LimitData(
                maxNumTweets: experimentParameters.UseOnlyTweetsWithGoldLabels
                    ? fullTrainingData.NumGoldTweets
                    : experimentParameters.NumberOfTrainingTweets,
                randomSeed: experimentParameters.RandomSeed);

            if (experimentParameters.UseBalancedTrainingSets)
                trainingData = (CrowdDataWithText)fullTrainingData.LimitData(
                    maxNumTweets: experimentParameters.UseOnlyTweetsWithGoldLabels
                        ? fullTrainingData.NumGoldTweets
                        : experimentParameters.NumberOfTrainingTweets,
                    balanceTweetsByLabel: true,
                    randomSeed: experimentParameters.RandomSeed);

            var validationData = (CrowdDataWithText)fullValidationData.LimitData(
                experimentParameters.NumberOfValidationJudgments,
                randomSeed: experimentParameters.RandomSeed);

            var validationDataWithNoWorkerLabels = (CrowdDataWithText)validationData.LimitData(maxNumWorkers: 0);

            var trainingWorkerMetrics = GetWorkerMetrics(trainingData, null, experimentParameters.MaximumNumberWorkers);
            var validationWorkerMetrics =
                GetWorkerMetrics(validationData, null, experimentParameters.MaximumNumberWorkers);
            var corpus = trainingData.TweetTexts.Values.Distinct().ToArray();
            var corpusInformation = CorpusInformation.BuildCorpusInformation(
                corpus,
                experimentParameters.VocabularyThreshold);

            var totalNumTrainingJudgments = trainingData.CrowdLabels.Count;
            var numTrainingJudgments = Util.ArrayInit(
                experimentParameters.NumDataSizes,
                i => Math.Round(totalNumTrainingJudgments * (i + 1) / (double)experimentParameters.NumDataSizes));

            var trainingDataSets = Util.ArrayInit(
                experimentParameters.NumDataSizes,
                i => (CrowdDataWithText)trainingData.LimitData((int)numTrainingJudgments[i],
                    randomSeed: experimentParameters.RandomSeed));

            var trainingTweets = trainingData.Tweets;
            var trainingCrowdWorkers = trainingData.Workers;
            var validationTweets = validationData.Tweets;
            var validationCrowdWorkers = validationData.Workers;

            var allDataInfo = new Dictionary<string, object>
            {
                ["Full training data"] = fullTrainingData,
                ["Full validation data"] = fullValidationData,
                ["Validation data"] = validationData,
                ["Training data"] = trainingData,
                ["Training data sets"] = trainingDataSets,
                ["Validation data set"] = validationData,
                ["Training set tweets"] = trainingTweets,
                ["Training set crowd workers"] = trainingCrowdWorkers,
                ["Validation set tweets"] = validationTweets,
                ["Validation set crowd workers"] = validationCrowdWorkers,
                ["CorpusInformation"] = corpusInformation
            };

            outputter.Out(allDataInfo, "Inputs");

            WriteCrowdDataDetailsToConsole(fullTrainingData, "Full training data");
            WriteCrowdDataDetailsToConsole(fullValidationData, "Full validation data");
            WriteCrowdDataDetailsToConsole(trainingData, "Training set");
            WriteCrowdDataDetailsToConsole(validationData, "Validation set");

            var now = DateTime.Now;
            var nowStr = $"{now.Year:D4}{now.Month:D2}{now.Day:D2}T{now.Hour:D2}{now.Minute:D2}{now.Second:D2}";

            var experimentInfo = new Dictionary<string, object>
            {
                ["Date"] = nowStr,
                ["Parameters"] = experimentParameters,
                [$"Training {WorkerMetricsKey}"] = trainingWorkerMetrics,
                [$"Validation {WorkerMetricsKey}"] = validationWorkerMetrics,
                [$"Training {LabelCountHistogramKey}"] = GetWorkerLabelCountHistogram(trainingData),
                [$"Validation {LabelCountHistogramKey}"] = GetWorkerLabelCountHistogram(validationData)
            };
            outputter.Out(experimentInfo, "Experiment");

            // Data structures for comparing results
            var metricsForPlots = new List<Metric> { Metric.Accuracy, Metric.AverageLogProb };
            var metricsStripCharts = new Dictionary<string, Dictionary<string, PointWithBounds[]>>();
            var metricsBarCharts = new Dictionary<string, Dictionary<string, double>>();
            foreach (var metric in metricsForPlots)
            {
                var metricString = metric.ToString();
                metricsStripCharts[metricString] = new Dictionary<string, PointWithBounds[]>();
                metricsBarCharts[metricString] = new Dictionary<string, double>();
            }

            // Creates a snapshot of currently accumulated comparison metrics
            Dictionary<string, object> getCurrentComparisonSnapshot()
            {
                var metricsStripChartsSnapshot =
                    new Dictionary<string, Dictionary<string, PointWithBounds[]>>();

                var metricsBarChartsSnapshot =
                    new Dictionary<string, Dictionary<string, double>>();

                foreach (var metric in metricsForPlots)
                {
                    var metricString = metric.ToString();
                    metricsStripChartsSnapshot[metricString] = new Dictionary<string, PointWithBounds[]>(metricsStripCharts[metricString]);
                    metricsBarChartsSnapshot[metricString] = new Dictionary<string, double>(metricsBarCharts[metricString]);
                }
                return new Dictionary<string, object>
                {
                    ["Strip Charts"] = metricsStripChartsSnapshot,
                    ["Bar Charts"] = metricsBarChartsSnapshot
                };
            }

            // Running models as specified in experiment parameters
            bool section2Started = false;
            Dictionary<string, object> section2Comparison = null;
            if ((experimentParameters.ModelTypes & ModelTypes.MajorityVote) != 0)
            {
                Console.WriteLine($"\n{Contents.S2TryingOutTheWorkerModel.NumberedName}\n");
                section2Started = true;
                var accuracyString = Metric.Accuracy.ToString();
                var majorityVoteString = "Majority Vote";
                var majorityVoteMetrics = RunMajorityVoteModel(validationData, majorityVoteString);
                outputter.Out(majorityVoteMetrics, Contents.S2TryingOutTheWorkerModel.NumberedName, majorityVoteString);

                metricsStripCharts[accuracyString][majorityVoteString] = GetChartPointsForMetric(
                    Enumerable.Repeat(majorityVoteMetrics, trainingDataSets.Length).ToList(),
                    Metric.Accuracy.ToString(),
                    numTrainingJudgments);

                metricsBarCharts[accuracyString][majorityVoteString] =
                    metricsStripCharts[accuracyString][majorityVoteString].Last().Y;

                section2Comparison = getCurrentComparisonSnapshot();
                outputter.Out(section2Comparison, Contents.S2TryingOutTheWorkerModel.NumberedName, "Comparison");
            }

            if ((experimentParameters.ModelTypes & ModelTypes.HonestWorker) != 0)
            {
                if (!section2Started)
                    Console.WriteLine($"\n{Contents.S2TryingOutTheWorkerModel.NumberedName}\n");
                var currentModel = new HonestWorkerModel(engine);
                var honestWorkerMetrics = new Dictionary<string, Dictionary<string, object>>();
                outputter.Out(honestWorkerMetrics, Contents.S2TryingOutTheWorkerModel.NumberedName, currentModel.Name);

                honestWorkerMetrics = RunSweep(
                    trainingDataSets,
                    validationData,
                    currentModel,
                    corpusInformation,
                    (map, model, runner) => new HonestWorkerRunner(map, (HonestWorkerModel)model, runner),
                    GetHonestWorkerTrainingResults,
                    (runner, metrics) =>
                        GetHonestWorkerValidationResults(runner, metrics, experimentParameters.MaximumNumberWorkers),
                    experimentParameters,
                    resultStorage: honestWorkerMetrics).Results;


                foreach (var metric in metricsForPlots)
                {
                    metricsStripCharts[metric.ToString()][currentModel.Name] =
                        GetChartPointsForMetric(honestWorkerMetrics, metric, numTrainingJudgments);
                    metricsBarCharts[metric.ToString()][currentModel.Name] =
                        metricsStripCharts[metric.ToString()][currentModel.Name].Last().Y;
                }

                section2Comparison = getCurrentComparisonSnapshot();
                outputter.Out(section2Comparison, Contents.S2TryingOutTheWorkerModel.NumberedName, "Comparison");
            }

            if ((experimentParameters.ModelTypes & ModelTypes.BiasedWorker) != 0)
            {
                Console.WriteLine($"\n{Contents.S3CorrectingForWorkerBiases.NumberedName}\n");
                var currentModel = new BiasedWorkerModel(engine);
                var biasedWorkerMetrics = new Dictionary<string, Dictionary<string, object>>();
                outputter.Out(biasedWorkerMetrics, Contents.S3CorrectingForWorkerBiases.NumberedName, currentModel.Name);

                biasedWorkerMetrics = RunSweep(
                    trainingDataSets,
                    validationData,
                    currentModel,
                    corpusInformation,
                    (map, model, runner) => new BiasedWorkerModelRunner(map, (BiasedWorkerModel)model, runner),
                    runner => GetBiasedWorkerTrainingResults(runner, experimentParameters.MaximumNumberWorkers),
                    GetValidationResults,
                    experimentParameters,
                    resultStorage: biasedWorkerMetrics).Results;

                foreach (var metric in metricsForPlots)
                {
                    metricsStripCharts[metric.ToString()][currentModel.Name] =
                        GetChartPointsForMetric(biasedWorkerMetrics, metric, numTrainingJudgments);
                    metricsBarCharts[metric.ToString()][currentModel.Name] =
                        metricsStripCharts[metric.ToString()][currentModel.Name].Last().Y;
                }

                outputter.Out(getCurrentComparisonSnapshot(), Contents.S3CorrectingForWorkerBiases.NumberedName, "Comparison");
            }

            if ((experimentParameters.ModelTypes & ModelTypes.BiasedCommunity) != 0)
            {
                Console.WriteLine($"\n{Contents.S4CommunitiesOfWorkers.NumberedName}\n");
                engine.Algorithm = new VariationalMessagePassing();
                foreach (int numCommunities in experimentParameters.NumberOfCommunitiesSweep)
                {
                    var currentModel = new BiasedCommunityModel(engine) { NumberOfCommunities = numCommunities };
                    var result = new Dictionary<string, Dictionary<string, object>>();
                    outputter.Out(result, Contents.S4CommunitiesOfWorkers.NumberedName, currentModel.Name);

                    result = RunSweep(
                        trainingDataSets,
                        validationData,
                        currentModel,
                        corpusInformation,
                        (map, model, runner) => new BiasedCommunityModelRunner(
                            map,
                            (BiasedCommunityModel)model,
                            runner),
                        GetBiasedCommunityTrainingResults,
                        GetValidationResults,
                        experimentParameters,
                        resultStorage: result).Results;

                    foreach (var metric in metricsForPlots)
                    {
                        metricsStripCharts[metric.ToString()][currentModel.Name] =
                            GetChartPointsForMetric(result, metric, numTrainingJudgments);
                        metricsBarCharts[metric.ToString()][currentModel.Name] =
                            metricsStripCharts[metric.ToString()][currentModel.Name].Last().Y;
                    }
                }

                outputter.Out(getCurrentComparisonSnapshot(), Contents.S4CommunitiesOfWorkers.NumberedName, "Comparison");
            }

            if ((experimentParameters.ModelTypes & ModelTypes.BiasedCommunityWords) != 0)
            {
                Console.WriteLine($"\n{Contents.S5MakingUseOfTheTweets.NumberedName}\n");
                // VMP is an order of magnitude faster than EP here, while producing almost the same result
                engine.Algorithm = new VariationalMessagePassing();
                foreach (int numCommunities in experimentParameters.NumberOfCommunitiesSweep)
                {
                    var currentModel = new BiasedCommunityWordsModel(engine) { NumberOfCommunities = numCommunities };
                    var result = new Dictionary<string, Dictionary<string, object>>();
                    outputter.Out(result, Contents.S5MakingUseOfTheTweets.NumberedName, currentModel.Name);

                    result = RunSweep(
                        trainingDataSets,
                        validationData,
                        currentModel,
                        corpusInformation,
                        (map, model, runner) => new BiasedCommunityWordsRunner(
                            (CrowdDataWithTextMapping)map,
                            (BiasedCommunityWordsModel)model,
                            runner),
                        GetBiasedCommunityWordsRunnerResults,
                        GetValidationResults,
                        experimentParameters,
                        validationDataWithNoWorkerLabels,
                        null,
                        50,
                        resultStorage: result).Results;

                    foreach (var metric in metricsForPlots)
                    {
                        metricsStripCharts[metric.ToString()][currentModel.Name] =
                            GetChartPointsForMetric(result, metric, numTrainingJudgments);
                        metricsBarCharts[metric.ToString()][currentModel.Name] =
                            metricsStripCharts[metric.ToString()][currentModel.Name].Last().Y;
                    }
                }

                outputter.Out(getCurrentComparisonSnapshot(), Contents.S5MakingUseOfTheTweets.NumberedName, "Comparison");
            }

            Console.WriteLine("\nCompleted all experiments.");
        }

        private static void InitializeUI()
        {
#if NETFULL
            InferenceEngine.Visualizer = new Microsoft.ML.Probabilistic.Compiler.Visualizers.WindowsVisualizer();
#endif
        }

        /// <summary>
        ///     Returns the results from the inference.
        /// </summary>
        /// <param name="results">
        ///     The metrics.
        /// </param>
        /// <param name="metric">
        ///     The metric.
        /// </param>
        /// <param name="independentValues">
        ///     The independent values.
        /// </param>
        /// <returns>
        ///     An array of chart points.
        /// </returns>
        public static PointWithBounds[] GetChartPointsForMetric(
            Dictionary<string, Dictionary<string, object>> results,
            Metric metric,
            double[] independentValues)
        {
            var validationResults = results.Select(
                res => Tuple.Create(
                    res.Key,
                    (double)((Dictionary<string, object>)res.Value[ValidationKey])[metric.ToString()])).OrderBy(
                tup =>
                {
                    var indexOfLastDash = tup.Item1.LastIndexOf("_", StringComparison.Ordinal);
                    if (indexOfLastDash < 0) throw new ApplicationException("Model name must be suffixed by a number");

                    return int.Parse(tup.Item1.Substring(indexOfLastDash + 1));
                }).Select(tup => 100.0 * tup.Item2).ToArray();

            return validationResults.Select((val, i) => new PointWithBounds(independentValues[i], val, 0.0)).ToArray();
        }

        /// <summary>
        ///     Returns the chart points from the metrics.
        /// </summary>
        /// <param name="metrics">
        ///     The metrics.
        /// </param>
        /// <param name="metric">
        ///     The metric.
        /// </param>
        /// <param name="independentValues">
        ///     The independent values.
        /// </param>
        /// <returns>
        ///     An array of chart points.
        /// </returns>
        public static PointWithBounds[] GetChartPointsForMetric(
            List<Dictionary<string, object>> metrics,
            string metric,
            double[] independentValues)
        {
            return metrics.Take(independentValues.Length).Select(dict => 100.0 * (double)dict[metric])
                .Select((val, i) => new PointWithBounds(independentValues[i], val, 0.0)).ToArray();
        }

        
    }
}