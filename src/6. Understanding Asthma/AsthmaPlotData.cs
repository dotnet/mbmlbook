// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
namespace UnderstandingAsthma
{
    using Microsoft.Research.Glo.Views;
    using Microsoft.ML.Probabilistic.Distributions;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public static class AsthmaPlotData
    {
        private static int intermediateLowIndex = 1;
        private static int intermediateHighIndex = 2;
        private static int[] lowCutoffsForSensitizationPlots = { 0, intermediateLowIndex, intermediateHighIndex };
        private static int[] highCutoffsForSensitizationPlots = { intermediateLowIndex, intermediateHighIndex, int.MaxValue };
        private static string[] seriesNamesForSensitizationPlots =
        {
            $"<{intermediateLowIndex} sensitization",
            $"{intermediateLowIndex}-{intermediateHighIndex} sensitizations",
            $">{intermediateHighIndex} sensitizations"
        };

        public static Dictionary<string, Dictionary<string, double>> GetDataCounts(AllergenData data)
        {
            int nT = AllergenData.NumTests;
            int nY = AllergenData.NumYears;
            return Enumerable.Range(0, data.Allergens.Count).ToDictionary(
                a => data.Allergens[a],
                a => Enumerable.Range(0, nT * nY).ToDictionary(
                    ty => AllergenData.Tests[ty % nT] + AllergenData.Years[ty / nT],
                    ty => (double)data.DataCountAllergenTestYear[a][ty % nT][ty / nT]));
        }

        public static Dictionary<string, Dictionary<string, Dictionary<string, double>>> GetSensitizationPerAllergenPerClass(AsthmaModel.Beliefs beliefs, List<string> allergens, bool usePercentages = false)
        {
            int nY = AllergenData.NumYears;
            int nA = allergens.Count;
            var childVulnerabilityClass = beliefs.VulnerabilityClass.Select(cl => cl.GetMode()).ToArray();
            var classCount = Enumerable.Range(0, beliefs.NumberOfClasses).Select(c => childVulnerabilityClass.Count(childVuln => childVuln == c)).ToArray();
            var sortedClassIndices = classCount.Select((classCnt, classIndex) => new { classCnt, classIndex }).OrderByDescending(ci => ci.classCnt).Select(ic => ic.classIndex).ToArray();
            string[] keys = BuildKeys(sortedClassIndices.Select(idx => classCount[idx]).ToArray());
            var percentInverseClassCount = classCount.Select(cnt => 100.0 / cnt).ToArray();
            return Enumerable.Range(0, beliefs.NumberOfClasses).ToDictionary(
                cx => keys[cx].ToString(),
                cx =>
                {
                    int c = sortedClassIndices[cx];
                    var indicesInClass = Enumerable.Range(0, beliefs.NumberOfChildren).Where(childIndex => childVulnerabilityClass[childIndex] == c);

                    // The sensitivity expectations accumulated over years
                    var sensitivityExpectationsAccumOverYears = indicesInClass.Select(childIndex =>
                            Enumerable.Range(0, nA).Select(
                                a => Enumerable.Range(0, nY).Sum(y => beliefs.Sensitization[y][childIndex, a].GetProbTrue())).ToArray());
                    return Enumerable.Range(0, seriesNamesForSensitizationPlots.Length).ToDictionary(
                        s => seriesNamesForSensitizationPlots[s],
                        s => Enumerable.Range(0, nA).ToDictionary(
                            a => allergens[a],
                            a => (usePercentages ? percentInverseClassCount[c] : 1.0) * sensitivityExpectationsAccumOverYears.Count(sc => sc[a] >= lowCutoffsForSensitizationPlots[s] && sc[a] < highCutoffsForSensitizationPlots[s])));
                });
        }

        private static string[] BuildKeys(int[] numbers)
        {
            var keys = new string[numbers.Length];
            keys[0] = numbers[0].ToString();
            for (int i = 1; i < numbers.Length; ++i)
            {
                keys[i] = numbers[i].ToString();
                int k = 1;
                while (keys[i] == keys[i - 1])
                {
                    keys[i] = $"{numbers[i]}_{k}";
                    ++k;
                }
            }

            return keys;
        }

        public static Dictionary<string, Dictionary<string, Dictionary<string, double>>> GetSensitizationPerYearPerClass(AsthmaModel.Beliefs beliefs, List<string> allergens, bool usePercentages = false)
        {
            int nY = AllergenData.NumYears;
            int nA = allergens.Count;
            var childVulnerabilityClass = beliefs.VulnerabilityClass.Select(cl => cl.GetMode()).ToArray();
            var classCount = Enumerable.Range(0, beliefs.NumberOfClasses).Select(c => childVulnerabilityClass.Count(childVuln => childVuln == c)).ToArray();
            var sortedClassIndices = classCount.Select((classCnt, classIndex) => new { classCnt, classIndex }).OrderByDescending(ci => ci.classCnt).Select(ic => ic.classIndex).ToArray();
            string[] keys = BuildKeys(sortedClassIndices.Select(idx => classCount[idx]).ToArray());
            var percentInverseClassCount = classCount.Select(cnt => 100.0 / cnt).ToArray();

            return Enumerable.Range(0, beliefs.NumberOfClasses).ToDictionary(
                cx => keys[cx].ToString(),
                cx =>
                {
                    int c = sortedClassIndices[cx];
                    var indicesInClass = Enumerable.Range(0, beliefs.NumberOfChildren).Where(childIndex => childVulnerabilityClass[childIndex] == c);

                    // The sensitivity counts accumulated over allergens
                    var sensitivityExpectationsAccumOverAllergens = indicesInClass.Select(childIndex =>
                            Enumerable.Range(0, nY).Select(
                                y => Enumerable.Range(0, nA).Sum(a => beliefs.Sensitization[y][childIndex, a].GetProbTrue())).ToArray());
                    return Enumerable.Range(0, seriesNamesForSensitizationPlots.Length).ToDictionary(
                        s => seriesNamesForSensitizationPlots[s],
                        s => Enumerable.Range(0, nY).ToDictionary(
                            y => AllergenData.Years[y],
                            y => (usePercentages ? percentInverseClassCount[c] : 1.0) * sensitivityExpectationsAccumOverAllergens.Count(sc => sc[y] >= lowCutoffsForSensitizationPlots[s] && sc[y] < highCutoffsForSensitizationPlots[s])));
                });
        }

        public static Dictionary<string, Dictionary<string, Dictionary<string, double>>> GetNumberOfChildrenWithInferredSensitization(AsthmaModel.Beliefs beliefs, List<string> allergens, bool usePercentages = false)
        {
            int nY = AllergenData.NumYears;
            int nA = allergens.Count;
            var childVulnerabilityClass = beliefs.VulnerabilityClass.Select(cl => cl.GetMode()).ToArray();
            var classCount = Enumerable.Range(0, beliefs.NumberOfClasses).Select(c => childVulnerabilityClass.Count(childVuln => childVuln == c)).ToArray();
            var sortedClassIndices = classCount.Select((classCnt, classIndex) => new { classCnt, classIndex }).OrderByDescending(ci => ci.classCnt).Select(ic => ic.classIndex).ToArray();
            string[] keys = BuildKeys(sortedClassIndices.Select(idx => classCount[idx]).ToArray());
            var percentInverseClassCount = classCount.Select(cnt => 100.0 / cnt).ToArray();

            return Enumerable.Range(0, beliefs.NumberOfClasses).ToDictionary(
                cx => keys[cx].ToString(),
                cx =>
                {
                    int c = sortedClassIndices[cx];
                    var indicesInClass = Enumerable.Range(0, beliefs.NumberOfChildren).Where(childIndex => childVulnerabilityClass[childIndex] == c);

                    return Enumerable.Range(0, nY).ToDictionary(
                        y => $"Age {AllergenData.Years[y]}",
                        y => Enumerable.Range(0, nA).ToDictionary(
                            a => allergens[a],
                            a => (usePercentages ? percentInverseClassCount[c] : 1.0) * indicesInClass.Count(childIndex => beliefs.Sensitization[y][childIndex, a].GetProbTrue() > 0.5)));
                });
        }

        private static Dictionary<string, Dictionary<string, Tuple<double, double, double>>> getChildrenCountsPerOutcome(AsthmaModel.Beliefs beliefs, AllergenData data, int[] outcomeIndicesToPlot)
        {
            var childVulnerabilityClass = beliefs.VulnerabilityClass.Select(cl => cl.GetMode()).ToArray();
            var classCount = Enumerable.Range(0, beliefs.NumberOfClasses).Select(c => childVulnerabilityClass.Count(childVuln => childVuln == c)).ToArray();
            var sortedClassIndices = classCount.Select((classCnt, classIndex) => new { classCnt, classIndex }).OrderByDescending(ci => ci.classCnt).Select(ic => ic.classIndex).ToArray();

            return Enumerable.Range(0, beliefs.NumberOfClasses).Where(cx => classCount[sortedClassIndices[cx]] > 0).ToDictionary(
                cx => "Class " + cx.ToString(),
                cx =>
                {
                    int c = sortedClassIndices[cx];
                    var indicesInClass = Enumerable.Range(0, beliefs.NumberOfChildren).Where(childIndex => childVulnerabilityClass[childIndex] == c);

                    return Enumerable.Range(0, outcomeIndicesToPlot.Length).ToDictionary(
                        o => data.OutcomeIndexToOutcomeName[outcomeIndicesToPlot[o]],
                        o =>
                        {
                            int oIdx = outcomeIndicesToPlot[o];
                            var postiveCount = indicesInClass.Count(childIndex => data.Outcomes[oIdx][childIndex] != null && data.Outcomes[oIdx][childIndex].Value == 1);
                            var negativeCount = indicesInClass.Count(childIndex => data.Outcomes[oIdx][childIndex] != null && data.Outcomes[oIdx][childIndex].Value == 0);
                            var nullCount = indicesInClass.Count(childIndex => data.Outcomes[oIdx][childIndex] == null);
                            return Tuple.Create((double)postiveCount, (double)negativeCount, (double)nullCount);
                        });
                });

        }

        public static Dictionary<string, Dictionary<string, double>> GetPercentageChildrenWithOutcome(AsthmaModel.Beliefs beliefs, AllergenData data, int[] outcomeIndicesToPlot)
        {
            var childrenCountsPerOutcome = getChildrenCountsPerOutcome(beliefs, data, outcomeIndicesToPlot);
            return childrenCountsPerOutcome.ToDictionary(
                kvp => kvp.Key, kvp => kvp.Value.ToDictionary(
                    kvp1 => kvp1.Key,
                    kvp1 =>
                    {
                        var denom = kvp1.Value.Item1 + kvp1.Value.Item2;
                        return denom == 0.0 ? 0.0 : 100.00 * kvp1.Value.Item1 / denom;
                    }));
        }

        public static Dictionary<string, Dictionary<string, string>> GetPlusMinusStringChildrenWithOutcome(AsthmaModel.Beliefs beliefs, AllergenData data, int[] outcomeIndicesToPlot)
        {
            var childrenCountsPerOutcome = getChildrenCountsPerOutcome(beliefs, data, outcomeIndicesToPlot);
            return childrenCountsPerOutcome.ToDictionary(
                kvp => kvp.Key, kvp => kvp.Value.ToDictionary(
                    kvp1 => kvp1.Key, 
                    kvp1 => PlusMinusString(new Beta(kvp1.Value.Item1 + 1, kvp1.Value.Item2 + 1))));
        }
        
        public static double BetaQuantile(Beta beta, double desiredCdfValue)
        {
            const double Steps = 100000;
            double cdf = 0.0;
            for (int i = 0; i < Steps; i++)
            {
                double x = i / Steps;
                double pdf = Math.Exp(beta.GetLogProb(x));
                cdf += pdf / Steps;
                if (cdf > desiredCdfValue)
                {
                    return x;
                }
            }

            return 1.0;
        }

        public static Dictionary<string, Dictionary<string, PointWithBounds[]>> GetTransitionProbabilities(AsthmaModel.Beliefs beliefs, bool getRetain, List<string> allergens)
        {
            int nY = AllergenData.NumYears;
            int nA = allergens.Count;
            var childVulnerabilityClass = beliefs.VulnerabilityClass.Select(cl => cl.GetMode()).ToArray();
            var classCount = Enumerable.Range(0, beliefs.NumberOfClasses).Select(c => childVulnerabilityClass.Count(childVuln => childVuln == c)).ToArray();
            var sortedClassIndices = classCount.Select((classCnt, classIndex) => new { classCnt, classIndex }).OrderByDescending(ci => ci.classCnt).Select(ic => ic.classIndex).ToArray();
            string[] keys = BuildKeys(sortedClassIndices.Select(idx => classCount[idx]).ToArray());
            return Enumerable.Range(0, beliefs.NumberOfClasses).ToDictionary(
                cx => keys[cx].ToString(),
                cx =>
                {
                    return Enumerable.Range(0, nA).ToDictionary(
                    a => allergens[a],
                    a =>
                    {
                        int startY = getRetain ? 1 : 0;
                        int c = sortedClassIndices[cx];
                        return Enumerable.Range(startY, nY - startY).Select(
                            y =>
                            {
                                var beta = y == 0 ?
                                    beliefs.ProbSensitizationAgeOne[a, c] :
                                    getRetain ? beliefs.ProbRetainSensitization[y][a, c] : beliefs.ProbGainSensitization[y][a, c];
                                return new PointWithBounds(
                                    int.Parse(AllergenData.Years[y]),
                                    beta.GetMean(),
                                    BetaQuantile(beta, 0.25),
                                    BetaQuantile(beta, 0.75));
                            }).ToArray();
                    });
                }
            );
        }

        public static Dictionary<string, IList> GetConditionalProbsOfPositiveTest(AsthmaModel.Beliefs beliefs)
        {
            var result = new Dictionary<string, IList>();
            result[""] = new List<string> { "Prob. of Pos. Skin Test", "Prob. of Pos. IgE Test", };
            result["If Sensitized"] = new List<Beta> { beliefs.ProbSkinIfSensitized, beliefs.ProbIgEIfSensitized };
            result["If Not Sensitized"] = new List<Beta> { beliefs.ProbSkinIfNotSensitized, beliefs.ProbIgEIfNotSensitized };

            return result;
        }

        public static Dictionary<string, IList> GetConditionalProbsOfPositiveTestAsStrings(AsthmaModel.Beliefs beliefs)
        {
            var result = new Dictionary<string, IList>();
            result[""] = new List<string> { "Prob. of Pos. Skin Test", "Prob. of Pos. IgE Test", };
            result["If Sensitized"] = new List<string>
            {
                PlusMinusString(beliefs.ProbSkinIfSensitized),
                PlusMinusString(beliefs.ProbIgEIfSensitized)
            };

            result["If Not Sensitized"] = new List<string>
            {
                PlusMinusString(beliefs.ProbSkinIfNotSensitized),
                PlusMinusString(beliefs.ProbIgEIfNotSensitized)
            };

            return result;
        }

        private static string PlusMinusString(Beta beta)
        {
            var lower = BetaQuantile(beta, 0.25);
            var upper = BetaQuantile(beta, 0.75);
            if (lower < 0.0)
            {
                lower = 0.0;
            }

            if (upper > 1)
            {
                upper = 1;
            }

            var middle = 0.5 * (lower + upper);
            var delta = upper - middle;

            // We only want one decimal place if possible on the delta
            // i.e. 3 decimal places in the percent world.
            // But we don't want a 0.

            double roundedDelta = delta;
            for (int numDigits = 3; numDigits < 10; numDigits++)
            {
                roundedDelta = Math.Round(delta, numDigits);
                if (roundedDelta > 0.0)
                {
                    break;
                }
            }

            return $"{middle:0.0%}±{roundedDelta:0.####%}";
        }
    }
}
