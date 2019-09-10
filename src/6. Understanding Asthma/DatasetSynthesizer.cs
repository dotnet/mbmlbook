// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//This file contains data structures and methods used to generate synthetic data for the Asthma model.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using MBMLViews;
using Microsoft.Research.Glo.Views;

namespace UnderstandingAsthma
{
    struct GainRetainProbability
    {
        public double ProbGain { get; }
        public double ProbRetain { get; }
        public GainRetainProbability(double probGain, double probRetain)
        {
            ProbGain = probGain;
            ProbRetain = probRetain;
        }
    }

    class GainRetainProbabilitySeries
    {
        public double ProbSens { get; }
        public ImmutableArray<GainRetainProbability> GainRetainSeries { get; }

        public GainRetainProbabilitySeries(double probSens, ImmutableArray<GainRetainProbability> gainRetainSeries)
        {
            if (gainRetainSeries == null)
                throw new ArgumentNullException(nameof(gainRetainSeries));

            ProbSens = probSens;
            GainRetainSeries = gainRetainSeries;
        }

        public GainRetainProbabilitySeries(double probSens, GainRetainProbability[] gainRetainSeries)
        {
            if (gainRetainSeries == null)
                throw new ArgumentNullException(nameof(gainRetainSeries));

            ProbSens = probSens;
            GainRetainSeries = gainRetainSeries.ToImmutableArray();
        }
    }

    class SensitizationClass
    {
        public ImmutableArray<string> Allergens { get; }
        public ImmutableArray<string> Years { get; }
        public ImmutableArray<string> Outcomes { get; }
        public ImmutableArray<GainRetainProbabilitySeries> SensitizationProbabilities { get; }
        public ImmutableArray<double> OutcomeProbabilities { get; }

        public SensitizationClass(
            ImmutableArray<string> allergens,
            ImmutableArray<string> years,
            ImmutableArray<GainRetainProbabilitySeries> sensitizationProbabilities,
            ImmutableArray<string> outcomes,
            ImmutableArray<double> outcomeProbabilities
            )
        {
            if (allergens == null)
                throw new ArgumentNullException(nameof(allergens));
            if (years == null)
                throw new ArgumentNullException(nameof(years));
            if (sensitizationProbabilities == null)
                throw new ArgumentNullException(nameof(sensitizationProbabilities));
            if (allergens.Length != sensitizationProbabilities.Length)
                throw new ArgumentException($"Numbers of provided allergens and sensitization probability series are inconsistent: lengths of {nameof(allergens)} and {nameof(sensitizationProbabilities)} must be equal, but they are not.");
            for (int i = 0; i < sensitizationProbabilities.Length; ++i)
                if (sensitizationProbabilities[i].GainRetainSeries.Length + 1 != years.Length)
                    throw new ArgumentException($"Inconsistent sensitization probability series: lengths of all series in {nameof(sensitizationProbabilities)} must be equal to the length of {nameof(years)}, but that's not true for {nameof(sensitizationProbabilities)}[{i}].");
            if (outcomes == null)
                throw new ArgumentNullException(nameof(outcomes));
            if (outcomeProbabilities == null)
                throw new ArgumentNullException(nameof(outcomeProbabilities));
            if (outcomes.Length != outcomeProbabilities.Length)
                throw new ArgumentException($"Numbers of provided outcomes and outcome probabilities are inconsistent: lengths of {nameof(outcomes)} and {nameof(outcomeProbabilities)} must be equal, but they are not.");

            Allergens = allergens;
            Years = years;
            SensitizationProbabilities = sensitizationProbabilities;
            Outcomes = outcomes;
            OutcomeProbabilities = outcomeProbabilities;
        }
    }

    class SensitizationClassCollection
    {
        public ImmutableArray<string> Allergens { get; }
        public ImmutableArray<string> Years { get; }
        public ImmutableArray<string> Outcomes { get; }
        public ImmutableArray<SensitizationClass> SensitizationClasses { get; }
        public ImmutableArray<int> SensitizationClassPopulations { get; }

        private SensitizationClassCollection(
            ImmutableArray<string> allergens,
            ImmutableArray<string> years,
            ImmutableArray<string> outcomes,
            ImmutableArray<SensitizationClass> sensitizationClasses,
            ImmutableArray<int> sensitizationClassPopulations
            )
        {
            Allergens = allergens;
            Years = years;
            Outcomes = outcomes;
            SensitizationClasses = sensitizationClasses;
            SensitizationClassPopulations = sensitizationClassPopulations;
        }

        public static SensitizationClassCollection CreateEmpty(
            ImmutableArray<string> allergens,
            ImmutableArray<string> years,
            ImmutableArray<string> outcomes
            )
        {
            return new SensitizationClassCollection(allergens, years, outcomes, ImmutableArray<SensitizationClass>.Empty, ImmutableArray<int>.Empty);
        }

        public static SensitizationClassCollection Create(
            ImmutableArray<string> allergens,
            ImmutableArray<string> years,
            ImmutableArray<string> outcomes,
            ImmutableArray<SensitizationClass> sensitizationClasses,
            ImmutableArray<int> sensitizationClassPopulations
            )
        {
            if (allergens == null)
                throw new ArgumentNullException(nameof(allergens));
            if (years == null)
                throw new ArgumentNullException(nameof(years));
            if (outcomes == null)
                throw new ArgumentNullException(nameof(outcomes));
            if (sensitizationClasses == null)
                throw new ArgumentNullException(nameof(sensitizationClasses));
            if (sensitizationClassPopulations == null)
                throw new ArgumentNullException(nameof(sensitizationClassPopulations));
            if (sensitizationClasses.Length != sensitizationClassPopulations.Length)
                throw new ArgumentException($"Numbers of provided sensitization classes and their populations are inconsistent: lengths of {nameof(sensitizationClasses)} and {nameof(sensitizationClassPopulations)} must be equal, but they are not.");
            for (int i = 0; i < sensitizationClasses.Length; ++i)
            {
                var sensClass = sensitizationClasses[i];
                if (sensClass.Allergens != allergens)
                    throw new ArgumentException($"Inconsistent allergen arrays: one in {nameof(sensitizationClasses)}[{i}] differs from {nameof(allergens)}.");
                if (sensClass.Years != years)
                    throw new ArgumentException($"Inconsistent allergen arrays: one in {nameof(sensitizationClasses)}[{i}] differs from {nameof(years)}.");
                if (sensClass.Outcomes != outcomes)
                    throw new ArgumentException($"Inconsistent allergen arrays: one in {nameof(sensitizationClasses)}[{i}] differs from {nameof(outcomes)}.");
            }

            return new SensitizationClassCollection(allergens, years, outcomes, sensitizationClasses, sensitizationClassPopulations);
        }

        public SensitizationClassCollection AddSensitizationClass(
            IEnumerable<GainRetainProbabilitySeries> sensitizationProbabilities,
            IEnumerable<double> outcomeProbabilities,
            int population
            )
        {
            if (sensitizationProbabilities == null)
                throw new ArgumentNullException(nameof(sensitizationProbabilities));
            if (outcomeProbabilities == null)
                throw new ArgumentNullException(nameof(outcomeProbabilities));

            var sensProbs = sensitizationProbabilities.ToImmutableArray();
            var outcomeProbs = outcomeProbabilities.ToImmutableArray();
            var sensClass = new SensitizationClass(Allergens, Years, sensProbs, Outcomes, outcomeProbs);
            var sensClasses = SensitizationClasses.Add(sensClass);
            var populations = SensitizationClassPopulations.Add(population);
            return new SensitizationClassCollection(Allergens, Years, Outcomes, sensClasses, populations);
        }
    }

    struct TestResultProbabilities
    {
        public double ProbabilityIfSensitized { get; }
        public double ProbabilityIfNotSensitized { get; }

        public TestResultProbabilities(double probabilityIfSensitized, double probabilityIfNotSensitized)
        {
            ProbabilityIfSensitized = probabilityIfSensitized;
            ProbabilityIfNotSensitized = probabilityIfNotSensitized;
        }
    }

    interface IDataMissingProbabilities
    {
        double GetMissingProbability(string testType, string age, string allergen, Dictionary<(string, string, string), bool> valueMissHistory);
    }

    class DefaultDataMissingProbabilities : IDataMissingProbabilities
    {
        public double GetMissingProbability(string testType, string age, string allergen, Dictionary<(string, string, string), bool> valueMissHistory)
        {
            double generalProb = 0.2;
            switch (testType)
            {
                case "Skin":
                    switch (age)
                    {
                        case "1":
                            switch (allergen)
                            {
                                case "Mould":
                                case "Peanut":
                                    return 1.0;
                                default:
                                    return 0.62;
                            }
                        case "3":
                            switch (allergen)
                            {
                                case "Peanut":
                                    return 1.0;
                                default:
                                    return generalProb;
                            }
                        case "5":
                            switch (allergen)
                            {
                                case "Peanut":
                                    return 1.0;
                                default:
                                    return generalProb;
                            }
                        default:
                            return generalProb;
                    }
                case "IgE":
                    bool skinMissing = false;
                    if (valueMissHistory.TryGetValue(("Skin", age, allergen), out bool extractedValue))
                        skinMissing = extractedValue;
                    double igeGeneral = skinMissing ? 1.0 : 0.375;
                    switch (age)
                    {
                        case "1":
                            switch (allergen)
                            {
                                case "Mould":
                                case "Peanut":
                                case "Pollen":
                                    return 1.0;
                                default:
                                    return skinMissing ? 1.0 : 0.5;
                            }
                        case "3":
                            switch (allergen)
                            {
                                case "Mould":
                                case "Peanut":
                                case "Pollen":
                                    return 1.0;
                                default:
                                    return skinMissing ? 1.0 : 0.78;
                            }
                        case "5":
                            switch (allergen)
                            {
                                case "Mould":
                                    return 1.0;
                                default:
                                    return igeGeneral;
                            }
                        case "8":
                            switch (allergen)
                            {
                                case "Mould":
                                    return 1.0;
                                default:
                                    return igeGeneral;
                            }
                        default:
                            return igeGeneral;
                    }
                default:
                    return generalProb;
            }
        }
    }

    class ChildBasedDataMissingProbabilities : IDataMissingProbabilities
    {
        public double GetMissingProbability(string testType, string age, string allergen, Dictionary<(string, string, string), bool> valueMissHistory)
        {
            double generalProb = 0.2;
            bool startedTests = false;
            if (valueMissHistory.Any(kvp => kvp.Key.Item2 == age && kvp.Key.Item1 == testType))
            {
                if (valueMissHistory.First(kvp => kvp.Key.Item2 == age && kvp.Key.Item1 == testType).Value)
                    return 1.0;
                else
                    startedTests = true;
            }
            switch (testType)
            {
                case "Skin":
                    switch (age)
                    {
                        case "1":
                            switch (allergen)
                            {
                                case "Mould":
                                case "Peanut":
                                    return 1.0;
                                default:
                                    return startedTests ? 0.0 : 0.62;
                            }
                        case "3":
                            switch (allergen)
                            {
                                case "Peanut":
                                    return 1.0;
                                default:
                                    return startedTests ? 0.0 : generalProb;
                            }
                        case "5":
                            switch (allergen)
                            {
                                case "Peanut":
                                    return 1.0;
                                default:
                                    return startedTests ? 0.0 : generalProb;
                            }
                        default:
                            return startedTests ? 0.0 : generalProb;
                    }
                case "IgE":
                    bool skinMissing = false;
                    if (valueMissHistory.TryGetValue(("Skin", age, allergen), out bool extractedValue))
                        skinMissing = extractedValue;
                    double igeGeneral = skinMissing ? 1.0 : 0.375;
                    switch (age)
                    {
                        case "1":
                            switch (allergen)
                            {
                                case "Mould":
                                case "Peanut":
                                case "Pollen":
                                    return 1.0;
                                default:
                                    return startedTests ? 0.0 : skinMissing ? 1.0 : 0.5;
                            }
                        case "3":
                            switch (allergen)
                            {
                                case "Mould":
                                case "Peanut":
                                case "Pollen":
                                    return 1.0;
                                default:
                                    return startedTests ? 0.0 : skinMissing ? 1.0 : 0.78;
                            }
                        case "5":
                            switch (allergen)
                            {
                                case "Mould":
                                    return 1.0;
                                default:
                                    return startedTests ? 0.0 : igeGeneral;
                            }
                        case "8":
                            switch (allergen)
                            {
                                case "Mould":
                                    return 1.0;
                                default:
                                    return startedTests ? 0.0 : igeGeneral;
                            }
                        default:
                            return startedTests ? 0.0 : igeGeneral;
                    }
                default:
                    return startedTests ? 0.0 : generalProb;
            }
        }
    }

    class DefaultMCARDataMissingProbabilities : IDataMissingProbabilities
    {
        public double GetMissingProbability(string testType, string age, string allergen, Dictionary<(string, string, string), bool> valueMissHistory)
        {
            double generalProb = 0.2;
            switch (testType)
            {
                case "Skin":
                    switch (age)
                    {
                        case "1":
                            switch (allergen)
                            {
                                case "Mould":
                                case "Peanut":
                                    return 1.0;
                                default:
                                    return 0.62;
                            }
                        case "3":
                            switch (allergen)
                            {
                                case "Peanut":
                                    return 1.0;
                                default:
                                    return generalProb;
                            }
                        case "5":
                            switch (allergen)
                            {
                                case "Peanut":
                                    return 1.0;
                                default:
                                    return generalProb;
                            }
                        default:
                            return generalProb;
                    }
                case "IgE":
                    var igeGeneralProb = 0.5;
                    switch (age)
                    {
                        case "1":
                            switch (allergen)
                            {
                                case "Mould":
                                case "Peanut":
                                case "Pollen":
                                    return 1.0;
                                default:
                                    return 0.81;
                            }
                        case "3":
                            switch (allergen)
                            {
                                case "Mould":
                                case "Peanut":
                                case "Pollen":
                                    return 1.0;
                                default:
                                    return 0.82;
                            }
                        case "5":
                            switch (allergen)
                            {
                                case "Mould":
                                    return 1.0;
                                default:
                                    return igeGeneralProb;
                            }
                        case "8":
                            switch (allergen)
                            {
                                case "Mould":
                                    return 1.0;
                                default:
                                    return igeGeneralProb;
                            }
                        default:
                            return igeGeneralProb;
                    }
                default:
                    return generalProb;
            }
        }
    }

    static class DatasetSynthesizer
    {
        private static IDictionary<string, TestResultProbabilities> defaultTests = null;
        public static IDictionary<string, TestResultProbabilities> DefaultTests
        {
            get
            {
                if (defaultTests == null)
                {
                    defaultTests = AllergenData.Tests.ToDictionary(
                        item => item,
                        item =>
                        {
                            switch (item)
                            {
                                case "Skin":
                                    return new TestResultProbabilities(0.647, 0.001);
                                case "IgE":
                                    return new TestResultProbabilities(0.894, 0.015);
                                default:
                                    throw new InvalidOperationException($"Unknown test in {nameof(AllergenData)}.Tests.");
                            }
                        });

                }
                return defaultTests;
            }
        }

        private static SensitizationClassCollection defaultSensitizationClassCollection = null;
        public static SensitizationClassCollection DefaultSensitizationClassCollection
        {
            get
            {
                if (defaultSensitizationClassCollection == null)
                {
                    // From AsthmaResults6 that itself was obtained by running the model on synthetic data
                    var allergens = AllergenData.AllergensInFile.ToImmutableArray();
                    var years = AllergenData.Years.ToImmutableArray();
                    var retProbIndices = Enumerable.Range(0, years.Length - 1).ToArray();
                    var outcomes = ImmutableArray.Create("Asthma");
                    defaultSensitizationClassCollection = SensitizationClassCollection.CreateEmpty(allergens, years, outcomes);
                    var resPath = "Data";
                    var data = FileUtils.Load<Dictionary<string, object>>(resPath, "AsthmaResults6");
                    var probGain = ((Dictionary<string, Dictionary<string, PointWithBounds[]>>)data["ProbabilityGainingSensitivity"]).ToArray();
                    var probRetain = ((Dictionary<string, Dictionary<string, PointWithBounds[]>>)data["ProbabilityRetainingSensitivity"]).ToArray();
                    var percentageOutcome = (Dictionary<string, Dictionary<string, double>>)data["PercentageChildrenWithOutcome"];
                    for (int i = 0; i < 4; ++i)
                    {
                        var population = int.Parse(probGain[i].Key);
                        defaultSensitizationClassCollection = defaultSensitizationClassCollection.AddSensitizationClass(
                            allergens.Select(allergen =>
                            {
                                if (probGain[i].Value.TryGetValue(allergen, out var gainProbs) && probRetain[i].Value.TryGetValue(allergen, out var retProbs))
                                {
                                    // Hacks to keep number of milk sensitizations small
                                    var probSens = allergen == "Milk" ? gainProbs[0].Lower / 2 : gainProbs[0].Y;
                                    return new GainRetainProbabilitySeries(
                                        probSens,
                                        retProbIndices.Select(
                                            j => new GainRetainProbability(allergen == "Milk" ? gainProbs[j + 1].Lower / 2 : gainProbs[j + 1].Y, retProbs[j].Y)).ToImmutableArray());
                                }

                                // No data - putting zero probs
                                return new GainRetainProbabilitySeries(
                                    0.0,
                                    ImmutableArray.Create(
                                        new GainRetainProbability(0.0, 0.0),
                                        new GainRetainProbability(0.0, 0.0),
                                        new GainRetainProbability(0.0, 0.0)));

                            }),
                            outcomes.Select(outcome => percentageOutcome["Class " + i.ToString()][outcome] / 100.0),
                            population);
                    }
                }
                return defaultSensitizationClassCollection;
            }
        }

        private static IDataMissingProbabilities defaultDataMissingProbabilities = null;
        public static IDataMissingProbabilities DefaultDataMissingProbabilities
        {
            get
            {
                if (defaultDataMissingProbabilities == null)
                {
                    defaultDataMissingProbabilities = new ChildBasedDataMissingProbabilities();
                }

                return defaultDataMissingProbabilities;
            }
        }

        public static void Synthesize(
            SensitizationClassCollection sensitizationClasses,
            IDictionary<string, TestResultProbabilities> tests,
            IDataMissingProbabilities missProbs,
            string outputTsvPath,
            int seed)
        {
            var outcomes = sensitizationClasses.Outcomes;
            var allergens = sensitizationClasses.Allergens;
            var years = sensitizationClasses.Years;
            var testsArray = tests.ToArray();
            int totalCols = outcomes.Length + testsArray.Length * allergens.Length * years.Length;
            var colNames = new List<string>(totalCols);
            colNames.AddRange(outcomes);
            foreach (var allergen in allergens)
                foreach (var year in years)
                    foreach (var test in testsArray)
                        colNames.Add($"{test.Key}_{allergen}_{year}");

            var rand = new Random(seed);

            int[] remainingByClasses = sensitizationClasses.SensitizationClassPopulations.ToArray();

            using (var writer = File.CreateText(outputTsvPath))
            {
                writer.WriteLine(string.Join("\t", colNames));
                for (int remaining = sensitizationClasses.SensitizationClassPopulations.Sum(); remaining > 0; --remaining)
                {
                    // randomly selecting the class for the next entry
                    int subjectNo = rand.Next(remaining), classNo = 0;
                    while (subjectNo >= remainingByClasses[classNo])
                    {
                        subjectNo -= remainingByClasses[classNo];
                        ++classNo;
                    }
                    --remainingByClasses[classNo];
                    var sensClass = sensitizationClasses.SensitizationClasses[classNo];

                    var values = new List<string>(totalCols);
                    var valueMissMask = new Dictionary<(string, string, string), bool>(totalCols);
                    for (int i = 0; i < outcomes.Length; ++i)
                        values.Add(rand.NextDouble() < sensClass.OutcomeProbabilities[i] ? "1" : "0");

                    for (int i = 0; i < allergens.Length; ++i)
                    {
                        bool sensitizedNow = false;
                        for (int j = 0; j < years.Length; ++j)
                        {
                            if (j == 0)
                                sensitizedNow = rand.NextDouble() < sensClass.SensitizationProbabilities[i].ProbSens;
                            else if (sensitizedNow)
                                sensitizedNow = rand.NextDouble() < sensClass.SensitizationProbabilities[i].GainRetainSeries[j - 1].ProbRetain;
                            else
                                sensitizedNow = rand.NextDouble() < sensClass.SensitizationProbabilities[i].GainRetainSeries[j - 1].ProbGain;

                            for (int k = 0; k < testsArray.Length; ++k)
                            {
                                var probs = testsArray[k].Value;
                                var missProb = missProbs.GetMissingProbability(testsArray[k].Key, years[j], allergens[i], valueMissMask);
                                if (rand.NextDouble() < missProb)
                                {
                                    values.Add("miss");
                                    valueMissMask.Add((testsArray[k].Key, years[j], allergens[i]), true);
                                }
                                else
                                {
                                    values.Add(rand.NextDouble() < (sensitizedNow ? probs.ProbabilityIfSensitized : probs.ProbabilityIfNotSensitized) ? "1" : "0");
                                    valueMissMask.Add((testsArray[k].Key, years[j], allergens[i]), false);
                                }
                            }
                        }
                    }
                    writer.WriteLine(string.Join("\t", values));
                }
            }
        }
    }
}
