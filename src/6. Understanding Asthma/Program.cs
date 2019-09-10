// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.Probabilistic.Models;
using Microsoft.ML.Probabilistic.Distributions;
using Microsoft.ML.Probabilistic.Math;
using Microsoft.ML.Probabilistic.Utilities;
using System.Threading.Tasks;
using MBMLViews;
using MBMLCommon;

namespace UnderstandingAsthma
{
    public class Program
    {
        const int AsthmaModelIterations = 200;

        public static void Main(string[] args)
        {
            InitializeUI();
            Outputter outputter = Outputter.GetOutputter(Contents.ChapterName);

            try
            {
                int[] numClassesForMultipleClassRuns = new int[] { 2, 3, 4, 5, 6 };
                RunExperiments(outputter, numClassesForMultipleClassRuns);
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
        
        /// </param>
        /// <summary>
        /// Runs experiments, takes results and shows them via outputter.
        /// </summary>
        /// <param name="outputter">A container for experiments output.</param>
        /// <param name="numClassesForMultiClassRuns">Numbers of classes to use in experiments.</param>
        public static void RunExperiments(Outputter outputter, int[] numClassesForMultipleClassRuns)
        {
            bool showFactorGraph = false;
            Rand.Restart(2);

            Console.WriteLine($"\n{Contents.S2TryingOutTheModel.NumberedName}.\n");
            Console.WriteLine("Running asthma model on synthetic data.");
            Console.WriteLine("Results will slightly differ from those in the book.");
            // Synthetic dataset was created using the following call
            //DatasetSynthesizer.Synthesize(
            //    DatasetSynthesizer.DefaultSensitizationClassCollection,
            //    DatasetSynthesizer.DefaultTests,
            //    DatasetSynthesizer.DefaultDataMissingProbabilities,
            //    Path.Combine("Data", "SyntheticDataset.tsv"),
            //    Rand.Int());
            AllergenData allData = new AllergenData();
            allData.LoadDataFromTabDelimitedFile(Path.Combine("Data", "SyntheticDataset.tsv"));
            outputter.Out(allData, Contents.S2TryingOutTheModel.NumberedName, "Asthma model", "Inputs");

            var dataCounts = AsthmaPlotData.GetDataCounts(allData);
            outputter.Out(dataCounts, Contents.S2TryingOutTheModel.NumberedName, "Asthma model", "DataCounts");

            // Remove mould and peanut allergens from following analysis.
            AllergenData data = AllergenData.WithAllergensRemoved(allData, new List<string> { "Mould", "Peanut" });

            int[] numClasses1Comp = new int[] { 1 };
            AsthmaModel.Beliefs[] trainingResults1Comp = RunTraining(data, numClasses1Comp, showFactorGraph);
            outputter.Out(trainingResults1Comp, Contents.S2TryingOutTheModel.NumberedName, "Asthma model", "TrainingResults");

            // Data for plots
            Dictionary<string, Dictionary<string, object>> results1Comp = BuildPlotsForAsthmaResults(allData, data, numClasses1Comp, trainingResults1Comp);

            outputter.Out(results1Comp, Contents.S2TryingOutTheModel.NumberedName, "Asthma model", "Plots");

            Console.WriteLine($"\n{Contents.S4ModellingWithGates.NumberedName}.\n");
            Rand.Restart(1);
            var trialResult = RunClinicalTrialExperiment(showFactorGraph);
            outputter.Out(trialResult, Contents.S4ModellingWithGates.NumberedName, "ClinicalTrialPlots");

            Console.WriteLine($"\n{Contents.S5DiscoveringSensitizationClasses.NumberedName}.\n");
            Console.WriteLine("Running asthma model on synthetic data.");
            Console.WriteLine("Results will slightly differ from those in the book.");
            outputter.Out(allData, Contents.S5DiscoveringSensitizationClasses.NumberedName, "Asthma model", "Inputs");
            outputter.Out(dataCounts, Contents.S5DiscoveringSensitizationClasses.NumberedName, "Asthma model", "DataCounts");

            Rand.Restart(3); // With this random seed we get exactly 4 classes in models allowing 5 or 6
                             // provided numClassesForMultipleClassRuns == new int[] { 2, 3, 4, 5, 6 }
                             // With other seeds we may get additional classes containing very few people, which is normal
            AsthmaModel.Beliefs[] trainingResults = RunTraining(data, numClassesForMultipleClassRuns, showFactorGraph);
            outputter.Out(trainingResults, Contents.S5DiscoveringSensitizationClasses.NumberedName, "Asthma model", "TrainingResults");

            // Data for plots
            Dictionary<string, Dictionary<string, object>> results = BuildPlotsForAsthmaResults(allData, data, numClassesForMultipleClassRuns, trainingResults);

            outputter.Out(results, Contents.S5DiscoveringSensitizationClasses.NumberedName, "Asthma model", "Plots");

            Console.WriteLine("\nCompleted all experiments.");
        }

        private static Dictionary<string, Dictionary<string, object>> BuildPlotsForAsthmaResults(AllergenData allData, AllergenData data, int[] numClasses, AsthmaModel.Beliefs[] trainingResults)
        {
            var allergenSensitizationPlots = trainingResults.Select(res => AsthmaPlotData.GetSensitizationPerAllergenPerClass(res, data.Allergens)).ToArray();
            var yearSensitizationPlots = trainingResults.Select(res => AsthmaPlotData.GetSensitizationPerYearPerClass(res, data.Allergens)).ToArray();
            var sensitizationCounts = trainingResults.Select(res => AsthmaPlotData.GetNumberOfChildrenWithInferredSensitization(res, data.Allergens)).ToArray();
            var probGainingSensitivity = trainingResults.Select(res => AsthmaPlotData.GetTransitionProbabilities(res, false, data.Allergens)).ToArray();
            var probRetainingSensitivity = trainingResults.Select(res => AsthmaPlotData.GetTransitionProbabilities(res, true, data.Allergens)).ToArray();
            var conditionalProbs = trainingResults.Select(res => AsthmaPlotData.GetConditionalProbsOfPositiveTestAsStrings(res)).ToArray();
            var outcomePercentagePlots = trainingResults.Select(res => AsthmaPlotData.GetPercentageChildrenWithOutcome(res, allData, new int[] { allData.OutcomeNameToOutcomeIndex["Asthma"] })).ToArray();
            var outcomePlusMinusPlots = trainingResults.Select(res => AsthmaPlotData.GetPlusMinusStringChildrenWithOutcome(res, allData, new int[] { allData.OutcomeNameToOutcomeIndex["Asthma"] })).ToArray();

            var results = Enumerable.Range(0, numClasses.Length).ToDictionary(
                c => "AsthmaResults" + numClasses[c],
                c =>
                {
                    var result = new Dictionary<string, object>();
                    result["AllergenSensitizationPlots"] = (object)allergenSensitizationPlots[c];
                    result["YearSensitizationPlots"] = (object)yearSensitizationPlots[c];
                    result["SensitizationPlots"] = (object)sensitizationCounts[c];
                    result["ProbabilityGainingSensitivity"] = (object)probGainingSensitivity[c];
                    result["ProbabilityRetainingSensitivity"] = (object)probRetainingSensitivity[c];
                    result["ConditionalProbsOfPositiveTest"] = conditionalProbs[c];
                    result["PercentageChildrenWithOutcome"] = outcomePercentagePlots[c];
                    result["PlusMinusChildrenWithOutcome"] = outcomePlusMinusPlots[c];
                    return result;
                });
            return results;
        }

        private static void InitializeUI()
        {
#if NETFULL
            InferenceEngine.Visualizer = new Microsoft.ML.Probabilistic.Compiler.Visualizers.WindowsVisualizer();
#endif
        }

        static AsthmaModel.Beliefs[] RunTraining(
            AllergenData data,
            int[] numberSensitizationClasses,
            bool showFactorGraph = false)
        {
            AsthmaModel.Beliefs[] result = new AsthmaModel.Beliefs[numberSensitizationClasses.Length];
            AsthmaModel[] model = Util.ArrayInit(
                numberSensitizationClasses.Length,
                n => new AsthmaModel("AsthmaTrainingModel_" + numberSensitizationClasses[n])
                {
                    Iterations = AsthmaModelIterations // Increase this for more classes.
                });

            Console.WriteLine("Model iteration progress");
            var reporter = new ConsoleMultiProgressReporter(
                Util.ArrayInit(numberSensitizationClasses.Length, n => "AsthmaTrainingModel_" + numberSensitizationClasses[n]),
                Util.ArrayInit(numberSensitizationClasses.Length, n => AsthmaModelIterations));

            // Randomly initialize messages externally to parallel loop so we remain deterministic.
            for (int n = 0; n < numberSensitizationClasses.Length; n++)
            {
                int idx = n; // for closure to work
                model[n].InitializeMessages(data, numberSensitizationClasses[n]);
                model[n].ProgressChanged += (e, p) => reporter.UpdateProgress(idx, p.Iteration + 1);
            }

            Parallel.For(0, numberSensitizationClasses.Length, n =>
            {
                result[n] = model[n].Run(data, numberSensitizationClasses[n], initializeMessages: false, showFactorGraph: showFactorGraph);
            });

            return result;
        }

        class ConsoleMultiProgressReporter
        {
            private readonly string[] totalStrings;
            private readonly int[] current;
            private readonly int numWidth;
            private readonly int colWidth;

            public ConsoleMultiProgressReporter(string[] modelNames, int[] maxProgressValues)
            {
                if (modelNames == null)
                    throw new ArgumentNullException(nameof(modelNames));
                if (maxProgressValues == null)
                    throw new ArgumentNullException(nameof(maxProgressValues));
                if (modelNames.Length != maxProgressValues.Length)
                    throw new ArgumentException($"Lengths of {nameof(modelNames)} and {nameof(maxProgressValues)} must be equal.");

                current = maxProgressValues.Select(_ => 0).ToArray();
                int maxModelNameWidth = modelNames.Max(s => s.Length);
                numWidth = maxProgressValues.Max().ToString().Length;
                colWidth = Math.Max(maxModelNameWidth, 2 * numWidth + 3); // "n / m".Length
                string[] models = modelNames.Select(s => s.PadRight(colWidth)).ToArray();
                totalStrings = maxProgressValues.Select(n => n.ToString().PadLeft(numWidth)).ToArray();

                string header = $"| {string.Join(" | ", models)} |";
                Console.WriteLine(header);
                Render();
            }

            public void UpdateProgress(int modelIndex, int progressValue)
            {
                if (modelIndex < 0 || modelIndex > current.Length)
                    throw new ArgumentException("Invalid model index", nameof(modelIndex));

                current[modelIndex] = progressValue;
                Render();
            }

            void Render()
            {
                string progressString = string.Join(
                    " | ",
                    Util.ArrayInit(current.Length, i => $"{current[i].ToString().PadLeft(numWidth)} / {totalStrings[i]}".PadRight(colWidth)));
                Console.WriteLine($"| {progressString} |");
            }
        }

        static Dictionary<string, object> RunClinicalTrialExperiment(bool showFactorGraph = false)
        {
            var numPatientsPerGroup = new int[] { 20, 60, 100 };
            var chartLabels = numPatientsPerGroup.Select(n => n.ToString()).ToArray();
            var clinicalTrialResults = numPatientsPerGroup.Select(n => RunClinicalTrialModel(n, showFactorGraph: showFactorGraph)).ToArray();
            var probOfGoodOutcomes =
                Enumerable.Range(0, numPatientsPerGroup.Length)
                .ToDictionary(
                    i => chartLabels[i],
                    i =>
                    {
                        var plots = new Dictionary<string, Beta>();
                        plots["p(probControl)"] = clinicalTrialResults[i].ProbIfControl;
                        plots["p(probTreated)"] = clinicalTrialResults[i].ProbIfTreated;
                        return plots;
                    });

            var probHasEffect =
                Enumerable.Range(0, numPatientsPerGroup.Length)
                .ToDictionary(
                    i => chartLabels[i],
                    i =>
                    {
                        var probs = new Dictionary<string, double>();
                        probs["NoEffect"] = clinicalTrialResults[i].TreatmentHasEffect.GetProbFalse();
                        probs["HasEffect"] = clinicalTrialResults[i].TreatmentHasEffect.GetProbTrue();
                        return probs;
                    });

            var result = new Dictionary<string, object>();
            result["ProbOfGoodOutcomeCharts"] = probOfGoodOutcomes;
            result["ProbHasEffect"] = probHasEffect;
            return result;
        }

        static ClinicalTrialModel.Posteriors RunClinicalTrialModel(
            int numTreatedPatientsInEachGroup,
            double fractionRecoveredControl = 0.4,
            double fractionRecoveredTreated = 0.65,
            bool showFactorGraph = false)
        {
            var recoveredControl = GenerateMockClinicalTrialData(numTreatedPatientsInEachGroup, fractionRecoveredControl);
            var recoveredTreated = GenerateMockClinicalTrialData(numTreatedPatientsInEachGroup, fractionRecoveredTreated);
            var model = new ClinicalTrialModel();
            return model.Run(recoveredControl, recoveredTreated, showFactorGraph);
        }

        static bool[] GenerateMockClinicalTrialData(int numPatients, double fractionRecovered)
        {
            int[] perm = Rand.Perm(numPatients);
            int numRecovered = (int)Math.Round(fractionRecovered * numPatients);
            return Util.ArrayInit(numPatients, p => perm[p] < numRecovered).ToArray();
        }
    }
}
