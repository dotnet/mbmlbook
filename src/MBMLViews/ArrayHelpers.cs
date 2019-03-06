// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLViews
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using MBMLViews.Views;
    using Microsoft.ML.Probabilistic.Math;

    /// <summary>
    /// The array helpers.
    /// </summary>
    public static class ArrayHelpers
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="dataArray">The data array.</param>
        /// <param name="i">The row index.</param>
        /// <param name="j">The column index.</param>
        /// <param name="cols">The number of columns.</param>
        /// <returns>The <see cref="object"/></returns>
        internal static object GetValue(Array dataArray, int i, int j, int cols)
        {
            if (dataArray.Rank == 1)
            {
                Array row = dataArray.GetValue(i) as Array;
                if (row != null)
                {
                    if (j < row.Length)
                    {
                        return row.GetValue(j);
                    }
                }
                else
                {
                    int index = (i * cols) + j;
                    if (index < dataArray.Length)
                    {
                        return dataArray.GetValue(index);
                    }
                }
            }
            else
            {
                return dataArray.GetValue(i, j);
            }

            return null;
        }

        /// <summary>
        /// Parses the array.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="getRangeFromData">if set to <c>true</c> [get range from data].</param>
        /// <param name="realRange">The real range.</param>
        /// <returns>The <see cref="MatrixCanvasViewModel"/></returns>
        internal static MatrixCanvasViewModel ParseArray(object obj, bool getRangeFromData, RealRange realRange)
        {
            if (obj == null)
            {
                return null;
            }

            var enumerable = obj as IEnumerable<Array>;
            if (enumerable != null)
            {
                return ParseArray(enumerable.ToArray(), getRangeFromData, realRange);
            }

            Array dataArray = obj as Array;
            if (dataArray != null)
            {
                return ParseArray(dataArray, getRangeFromData, realRange);
            }

            Matrix matrix = obj as Matrix;
            if (matrix != null)
            {
                return new MatrixCanvasViewModel
                           {
                               Rows = matrix.Rows,
                               Cols = matrix.Cols,
                               DataRange = new RealRange { Max = matrix.Max(), Min = matrix.Min() },
                               Data = matrix.SourceArray,
                               DataType = TypeCode.Double,
                               RowSums = (matrix * Vector.Constant(matrix.Cols, 1)).ToArray()
                           };
            }

            var vectors = obj as IList<Vector>;
            if (vectors != null)
            {
                return ParseVectorList(vectors);
            }

            var sparseVectors = obj as IList<SparseVector>;
            if (sparseVectors != null)
            {
                return ParseVectorList(sparseVectors);
            }

            return null;
        }

        /// <summary>
        /// Gets the indices from row sums.
        /// </summary>
        /// <param name="rowSums">The row sums.</param>
        /// <returns>
        /// The indices
        /// </returns>
        internal static IList<int> GetIndicesFromRowSums(IEnumerable<double> rowSums)
        {
            // Sort row sums in ascending order and store the indices
            var sorted = rowSums.Select((x, i) => new KeyValuePair<double, int>(x, i))
                .OrderBy(x => x.Key)
                .ToList();
            return sorted.Select(x => x.Value).ToList();
        }

        /// <summary>
        /// Checks the custom sort order validity.
        /// </summary>
        /// <param name="a">The list.</param>
        /// <param name="start">The start.</param>
        /// <param name="count">The count.</param>
        /// <returns>Whether the sort order is valid</returns>
        internal static bool CheckCustomSortOrderValidity(IList<int> a, int start, int count)
        {
            IList<int> b = Enumerable.Range(start, count).ToList();
            return (a.Count == b.Count) && new HashSet<int>(a).SetEquals(b);
        }

        /// <summary>
        /// Parses the vector list.
        /// </summary>
        /// <typeparam name="T">The type of the vectors</typeparam>
        /// <param name="vectors">The vectors.</param>
        /// <returns>
        /// The <see cref="MatrixCanvasViewModel" />
        /// </returns>
        private static MatrixCanvasViewModel ParseVectorList<T>(IList<T> vectors) where T : Vector
        {
            // Todo: make this more efficient if required
            return new MatrixCanvasViewModel
                       {
                           Rows = vectors.Count,
                           Cols = vectors.Select(ia => ia.Count).Max(),
                           DataRange =
                               new RealRange
                                   {
                                       Min = vectors.Select(ia => ia.Min()).Min(),
                                       Max = vectors.Select(ia => ia.Max()).Max()
                                   },
                           Data = vectors.Select(ia => ia.ToArray()).ToArray(),
                           DataType = TypeCode.Double,
                       };
        }

        /// <summary>
        /// Parses the array.
        /// </summary>
        /// <param name="dataArray">The data array.</param>
        /// <param name="getRangeFromData">if set to <c>true</c> [get range from data].</param>
        /// <param name="realRange">The real range.</param>
        /// <returns>The <see cref="MatrixCanvasViewModel"/></returns>
        /// <exception cref="System.ArgumentException">If getRangeFromData is false yMin and yMax must be specified</exception>
        private static MatrixCanvasViewModel ParseArray(Array dataArray, bool getRangeFromData, RealRange realRange)
        {
            MatrixCanvasViewModel viewModel = new MatrixCanvasViewModel
                                                  {
                                                      DataType = TypeCode.Empty,
                                                      Rows = 0,
                                                      Cols = 0,
                                                      DataRange = new RealRange(),
                                                      Data = dataArray
                                                  };

            if (getRangeFromData)
            {
                realRange = new RealRange();
            }
            else
            {
                if (realRange == null || !realRange.IsValid())
                {
                    throw new ArgumentException("If getRangeFromData is false yMin and yMax must be specified");
                }
            }

            if (dataArray == null)
            {
                return viewModel;
            }

            switch (dataArray.Rank)
            {
                case 1:
                    ParseJaggedArray(dataArray, getRangeFromData, realRange, ref viewModel);
                    break;
                case 2:
                    Parse2DArray(dataArray, getRangeFromData, realRange, ref viewModel);
                    break;
            }

            return viewModel;
        }

        /// <summary>
        /// Parses the jagged array.
        /// </summary>
        /// <param name="dataArray">The data array.</param>
        /// <param name="getRangeFromData">if set to <c>true</c> [get range from data].</param>
        /// <param name="realRange">The real range.</param>
        /// <param name="viewModel">The view model.</param>
        private static void ParseJaggedArray(Array dataArray, bool getRangeFromData, RealRange realRange, ref MatrixCanvasViewModel viewModel)
        {
            // This is a jagged array
            // First get the length of the longest row
            List<int> rowLengths = new List<int>();
            viewModel.RowSums = new double[dataArray.Length];

            for (int i = 0; i < dataArray.Length; i++)
            {
                Array row = dataArray.GetValue(i) as Array;
                if ((row == null) || (row.Rank == 0) || (row.Length == 0))
                {
                    rowLengths.Add(0);
                    continue;
                }

                if (row.Rank > 1)
                {
                    // Deeper than two dimensions - return without creating data object
                    viewModel.DataType = TypeCode.Empty;
                    break;
                }

                rowLengths.Add(row.Length);

                for (int j = 0; j < row.Length; j++)
                {
                    var val = row.GetValue(j);
                    if (viewModel.DataType == TypeCode.Empty && val != null)
                    {
                        viewModel.DataType = Type.GetTypeCode(val.GetType());
                    }

                    double dval = Convert.ToDouble(val);
                    viewModel.RowSums[i] += dval;

                    if (!getRangeFromData)
                    {
                        continue;
                    }

                    realRange.Expand(dval);
                }
            }

            realRange.FixErrors();
            viewModel.RowLengths = rowLengths.ToArray();
            viewModel.DataRange = realRange;
            viewModel.Rows = dataArray.Length;
            viewModel.Cols = rowLengths.Max();
        }

        /// <summary>
        /// Parses the 2d array.
        /// </summary>
        /// <param name="dataArray">The data array.</param>
        /// <param name="getRangeFromData">if set to <c>true</c> [get range from data].</param>
        /// <param name="realRange">The real range.</param>
        /// <param name="viewModel">The view model.</param>
        private static void Parse2DArray(Array dataArray, bool getRangeFromData, RealRange realRange, ref MatrixCanvasViewModel viewModel)
        {
            viewModel.RowSums = new double[dataArray.Length];

            for (int i = 0; i < dataArray.GetLength(0); i++)
            {
                for (int j = 0; j < dataArray.GetLength(1); j++)
                {
                    object val = dataArray.GetValue(i, j);
                    double dval = Convert.ToDouble(val);
                    viewModel.DataType = Type.GetTypeCode(val.GetType());
                    viewModel.RowSums[i] += dval;

                    if (!getRangeFromData)
                    {
                        continue;
                    }

                    realRange.Expand(dval);
                }
            }

            realRange.FixErrors();
            viewModel.DataRange = realRange;
            viewModel.Rows = dataArray.GetLength(0);
            viewModel.Cols = dataArray.GetLength(1);
        }
    }
}