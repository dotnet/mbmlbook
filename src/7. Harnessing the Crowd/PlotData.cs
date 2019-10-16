// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Research.Glo.Object;

namespace HarnessingTheCrowd
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;

    using Microsoft.Research.Glo.Views;

    using Microsoft.ML.Probabilistic.Collections;
    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Math;
    using Microsoft.ML.Probabilistic.Utilities;

    /// <summary>
    /// The plot data for the chapter on crowd sourcing.
    /// </summary>
    public static class PlotData
    {
        /// <summary>
        /// The conditional probability table row label.
        /// </summary>
        public const string ConfusionMatrixRowLabel = "True";

        /// <summary>
        /// The conditional probability table column label.
        /// </summary>
        public const string ConfusionMatrixColLabel = "Worker";

        /// <summary>
        /// Secondary conditional probability table column label.
        /// </summary>
        public const string ConfusionMatrixColLabel2 = "Inferred True"; 
        
        /// <summary>
        /// Gets the worker abilities as a histogram.
        /// </summary>
        /// <param name="runner">
        /// The runner.
        /// </param>
        /// <returns>
        /// The histogram of abilities.
        /// </returns>
        public static Dictionary<string, double> GetWorkerAbilities(HonestWorkerRunner runner)
        {
            const int NumBins = 50;
            var counts = Util.ArrayInit(NumBins, i => 0.0);
            var abilities = runner.WorkerAbility.Select(kvp => kvp.Value.GetMean() * NumBins).ToArray();

            abilities.ForEach(
                ability =>
                    {
                        var intBin = (int)Math.Floor(ability);
                        if (intBin < 0)
                        {
                            intBin = 0;
                        }

                        if (intBin >= NumBins)
                        {
                            intBin = NumBins - 1;
                        }

                        counts[intBin]++;
                    });

            return Util.ArrayInit(NumBins, i => i).ToDictionary(
                i => ((i + 0.5) / NumBins).ToString(CultureInfo.InvariantCulture),
                i => counts[i]);
        }

        /// <summary>
        /// Gets confusion matrix or conditional probability table plot data.
        /// </summary>
        /// <param name="array">
        /// The array representing the conditional probability table.
        /// </param>
        /// <param name="mapping">
        /// The data mapping.
        /// </param>
        /// <param name="rowLabel">
        /// The row label.
        /// </param>
        /// <param name="columnLabel">
        /// The column label.
        /// </param>
        /// <param name="asPercentages">Entries as percentages.</param>
        /// <returns>
        /// The conditional probability table plot data.
        /// </returns>
        public static Dictionary<string, List<object>> GetConfusionMatrix(
            double[,] array,
            CrowdDataMapping mapping,
            string rowLabel,
            string columnLabel,
            bool asPercentages = false)
        {
            Debug.Assert(
                array.GetLength(0) == mapping.LabelCount && array.GetLength(1) == mapping.LabelCount,
                "Inconsistent arguments");

            var result = new Dictionary<string, List<object>>();
            var labelIndexToString = mapping.LabelIndexToString;
            var rowSums = new double[mapping.LabelCount];
            var namesColumn = new List<object>();
            for (var row = 0; row < mapping.LabelCount; row++)
            {
                var rowName = $"{labelIndexToString[row]} ({rowLabel})";
                namesColumn.Add(rowName);
                for (var col = 0; col < mapping.LabelCount; col++)
                {
                    rowSums[row] += array[row, col];
                }
            }

            result[string.Empty] = namesColumn;
            for (var col = 0; col < mapping.LabelCount; col++)
            {
                var columnName = $"{labelIndexToString[col]} ({columnLabel})";
                var column = new List<object>();
                for (var row = 0; row < mapping.LabelCount; row++)
                {
                    var value = array[row, col];
                    if (asPercentages)
                    {
                        column.Add(new Percentage(value / rowSums[row]));
                    }
                    else
                    {
                        column.Add(value);
                    }
                }

                result[columnName] = column;
            }

            return result;
        }

        /// <summary>
        /// Gets conditional probability table plot data.
        /// </summary>
        /// <param name="cpt">
        /// The array representing the conditional probability table.
        /// </param>
        /// <param name="mapping">
        /// The data mapping.
        /// </param>
        /// <param name="rowLabel">
        /// The row label.
        /// </param>
        /// <param name="columnLabel">
        /// The column label.
        /// </param>
        /// <returns>
        /// The conditional probability table plot data.
        /// </returns>
        public static Dictionary<string, PointWithBounds<string>[]> GetCptWithBounds(
            Dirichlet[] cpt,
            CrowdDataMapping mapping,
            string rowLabel,
            string columnLabel)
        {
            Debug.Assert(
                cpt.Length == mapping.LabelCount && cpt.All(mat => mat.Dimension == mapping.LabelCount),
                "Inconsistent arguments");

            var result = new Dictionary<string, PointWithBounds<string>[]>();
            var labelIndexToString = mapping.LabelIndexToString;

            for (var i = 0; i < mapping.LabelCount; i++)
            {
                var rowName = $"{labelIndexToString[i]} ({rowLabel})";
                result[rowName] = DirichletWithErrorBars(cpt[i], labelIndexToString);
            }

            return result;
        }

        /// <summary>
        /// Gets conditional probability table plot data.
        /// </summary>
        /// <param name="workerCpts">
        /// The worker conditional probability tables.
        /// </param>
        /// <param name="mapping">
        /// The data mapping.
        /// </param>
        /// <param name="asPercentages">Entries as percentages</param>
        /// <returns>
        /// The conditional probability table plot data.
        /// </returns>
        public static Dictionary<string, Dictionary<string, List<object>>> GetWorkerCpts(
            Dictionary<string, Dirichlet[]> workerCpts,
            CrowdDataMapping mapping,
            bool asPercentages = false)
        {
            return workerCpts.ToDictionary(
                kvp => kvp.Key,
                kvp =>
                    {
                        var labelCount = kvp.Value.Length;
                        var meanConfusionMatrix = kvp.Value.Select(cm => cm.GetMean()).ToArray();
                        return GetConfusionMatrix(
                            Util.ArrayInit(labelCount, labelCount, (i, j) => meanConfusionMatrix[i][j]),
                            mapping,
                            ConfusionMatrixRowLabel,
                            ConfusionMatrixColLabel,
                            asPercentages);
                    });
        }


        /// <summary>
        /// Gets conditional probability table plot data.
        /// </summary>
        /// <param name="array">
        /// The array representing the conditional probability table.
        /// </param>
        /// <param name="mapping">
        /// The data mapping.
        /// </param>
        /// <param name="rowLabel">
        /// The row label.
        /// </param>
        /// <param name="columnLabel">
        /// The column label.
        /// </param>
        /// <returns>
        /// The conditional probability table plot data.
        /// </returns>
        public static Dictionary<string, Dictionary<string, List<Tweet>>> GetTweetMatrix(
            List<Tweet>[,] array,
            CrowdDataMapping mapping,
            string rowLabel,
            string columnLabel)
        {
            Debug.Assert(
                array.GetLength(0) == mapping.LabelCount && array.GetLength(1) == mapping.LabelCount,
                "Inconsistent arguments");

            var result = new Dictionary<string, Dictionary<string, List<Tweet>>>();

            for (var i = 0; i < mapping.LabelCount; i++)
            {
                var rowName = $"{mapping.LabelValueToString[mapping.LabelIndexToValue[i]]} ({rowLabel})";
                var row = new Dictionary<string, List<Tweet>>();

                for (var j = 0; j < mapping.LabelCount; j++)
                {
                    var colName = $"{mapping.LabelValueToString[mapping.LabelIndexToValue[j]]} ({columnLabel})";
                    row[colName] = array[i, j];
                }

                result[rowName] = row;
            }

            return result;
        }

        /// <summary>
        /// Returns an array of probabilities with error bars given a Dirichlet distribution.
        /// </summary>
        /// <param name="dir">
        /// The Dirichlet distribution.
        /// </param>
        /// <param name="labels">
        /// The labels.
        /// </param>
        /// <returns>
        /// The array of points.
        /// </returns>
        public static PointWithBounds<string>[] DirichletWithErrorBars(Dirichlet dir, string[] labels)
        {
            const double NumStandardDeviations = 2.0;
            var means = dir.GetMean();
            var variance = dir.GetVariance();
            return variance.Select(
                (v, i) =>
                    {
                        var sdev = Math.Sqrt(v);
                        var mean = means[i];
                        var deviation = NumStandardDeviations * sdev;
                        var upper = Math.Min(mean + deviation, 1.0);
                        var lower = Math.Max(mean - deviation, 0.0);
                        return new PointWithBounds<string>
                           {
                               X = labels[i],
                               Y = mean,
                               Upper = upper,
                               Lower = lower
                           };
                    }).ToArray();
        }
    }
}
