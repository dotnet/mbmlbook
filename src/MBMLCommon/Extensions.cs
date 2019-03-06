// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLViews
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using MBMLCommon;
    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Math;

    /// <summary>
    /// Commonly used Extensions.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// The coefficients for calculating the error function
        /// </summary>
        private static readonly double[] ErfCoefficients =
            {
                -1.26551223, 1.00002368, 0.37409196, 0.09678418, -0.18628806, 0.27886807,
                -1.13520398, 1.48851587, -0.82215223, 0.17087277
            };

        /// <summary>
        /// Deep copy of any serializable object.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="source">The source object.</param>
        /// <returns>The copy.</returns>
        public static T DeepCopy<T>(this T source)
        {
            using (var memoryStream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, source);
                memoryStream.Position = 0;

                return (T)formatter.Deserialize(memoryStream);
            }
        }

        /// <summary>
        /// Removes all.
        /// </summary>
        /// <typeparam name="T">The type of the collection.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="predicate">The predicate.</param>
        /// <exception cref="System.ArgumentNullException">
        /// collection
        /// or
        /// predicate
        /// </exception>
        public static void RemoveAll<T>(this ICollection<T> collection, Func<T, bool> predicate)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }

            collection.Where(predicate).ToList().ForEach(ia => collection.Remove(ia));
        }

        /// <summary>
        /// Adds the specified dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="keyValuePair">The key value pair.</param>
        /// <exception cref="System.ArgumentNullException">Dictionary is null.</exception>
        public static void Add<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, KeyValuePair<TKey, TValue> keyValuePair)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException("dictionary");
            }

            dictionary.Add(keyValuePair.Key, keyValuePair.Value);
        }
        
        /// <summary>
        /// To the dictionary for table.
        /// </summary>
        /// <typeparam name="T">The type of the collection.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="keyFunc">The key function.</param>
        /// <param name="valueFuncsWithHeaders">Value functions with the header text for that function.</param>
        /// <param name="transpose">if set to <c>true</c> [transpose].</param>
        /// <param name="objectAggregationFuncsWithHeaders">The aggregation functions with header text (e.g. Total, Average, Sum ...).</param>
        /// <param name="emptyKey">The empty key.</param>
        /// <returns>
        /// The dictionary.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">Wrong number of value functions.</exception>
        public static Dictionary<TKey, object> ToDictionaryForTable<T, TKey, TValue, TAggregate>(
            this IEnumerable<T> collection,
            Func<T, TKey> keyFunc,
            IEnumerable<KeyValuePair<TKey, Func<T, TValue>>> valueFuncsWithHeaders,
            bool transpose = true,
            IEnumerable<KeyValuePair<TKey, Func<IList<TValue>, TAggregate>>> objectAggregationFuncsWithHeaders = null,
            TKey emptyKey = default(TKey))
        {
            // ReSharper disable PossibleMultipleEnumeration - just checking if it has elements
            if (collection == null || !collection.Any() || keyFunc == null || valueFuncsWithHeaders == null)
            {
                return null;
            }

            var aggFuncsWithHeaders = objectAggregationFuncsWithHeaders == null
                                          ? new List<KeyValuePair<TKey, Func<IList<TValue>, TAggregate>>>()
                                          : objectAggregationFuncsWithHeaders as IList<KeyValuePair<TKey, Func<IList<TValue>, TAggregate>>>
                                            ?? objectAggregationFuncsWithHeaders.ToList();

            var funcsWithHeaders = valueFuncsWithHeaders as IList<KeyValuePair<TKey, Func<T, TValue>>> ?? valueFuncsWithHeaders.ToList();

            var headers = funcsWithHeaders.Select(ia => ia.Key).ToArray();
            var valueFuncs = funcsWithHeaders.Select(ia => ia.Value);

            var aggHeaders = aggFuncsWithHeaders.Select(ia => ia.Key).ToArray();
            var aggFuncs = aggFuncsWithHeaders.Select(ia => ia.Value);

            TKey[] keys = collection.Select(keyFunc).ToArray();
            TValue[][] values = valueFuncs.Select(f => collection.Select(f).ToArray()).ToArray();
            TAggregate[][] aggregates = aggFuncs.Select(f => values.Select(f).ToArray()).ToArray();

            // ReSharper restore PossibleMultipleEnumeration
            if (transpose)
            {
                // True
                var dictionary = new Dictionary<TKey, object> { { emptyKey, headers } };
                for (int i = 0; i < keys.Length; i++)
                {
                    dictionary.Add(keys[i], values.Select(ia => ia[i]).ToArray());
                }

                for (int i = 0; i < aggregates.Length; i++)
                {
                    dictionary.Add(aggHeaders[i], aggregates[i]);
                }

                return dictionary;
            }
            else
            {
                // False
                var dictionary = new Dictionary<TKey, object> { { emptyKey, keys.Concat(aggHeaders).ToArray() } };
                for (int i = 0; i < headers.Length; i++)
                {
                    List<object> v = values[i].Cast<object>().ToList();
                    var aggs = aggregates.Select(ia => (object)ia[i]).ToArray();
                    v.AddRange(aggs);
                    dictionary.Add(headers[i], v);
                }

                return dictionary;
            }
        }

        /// <summary>
        /// Determines whether [is implementation of] [the specified base type].
        /// </summary>
        /// <param name="baseType">Type of the base.</param>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <returns>The <see cref="bool"/></returns>
        public static bool IsImplementationOf(this Type baseType, Type interfaceType)
        {
            return baseType.GetInterfaces().Any(interfaceType.Equals);
        }

        /// <summary>
        /// Flattens the specified jagged array.
        /// </summary>
        /// <typeparam name="T">The array type.</typeparam>
        /// <param name="jaggedArray">The jagged array.</param>
        /// <returns>The flattened array.</returns>
        public static T[] Flatten<T>(this IEnumerable<T[]> jaggedArray)
        {
            return jaggedArray == null ? null : jaggedArray.SelectMany(inner => inner.ToArray()).ToArray();
        }

        /// <summary>
        /// Bins the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="numberOfBins">The number of bins.</param>
        /// <returns>The binned data.</returns>
        public static int[] Bin(this IEnumerable<double[]> data, int numberOfBins)
        {
            return data == null ? null : data.Flatten().Bin(numberOfBins);
        }

        /// <summary>
        /// Bins the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="numberOfBins">The number of bins.</param>
        /// <param name="min">The min.</param>
        /// <param name="max">The max.</param>
        /// <returns>The binned data.</returns>
        public static int[] Bin(this IEnumerable<double[]> data, int numberOfBins, double min, double max)
        {
            return data == null ? null : data.Flatten().Bin(numberOfBins, min, max);
        }

        /// <summary>
        /// Bins the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="numberOfBins">The number of bins.</param>
        /// <param name="binBoundaries">The bin boundaries.</param>
        /// <param name="binPositions">The bin positions.</param>
        /// <returns>The binned data.</returns>
        public static int[] Bin(this IEnumerable<double[]> data, int numberOfBins, out double[] binBoundaries, out int[] binPositions, out double binWidth)
        {
            if (data != null)
            {
                return data.Flatten().Bin(numberOfBins, out binBoundaries, out binPositions, out binWidth);
            }

            binBoundaries = null;
            binPositions = null;
            binWidth = double.NaN;
            return null;
        }

        /// <summary>
        /// Bins the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="numberOfBins">The number of bins.</param>
        /// <param name="min">The min.</param>
        /// <param name="max">The max.</param>
        /// <param name="binBoundaries">The bin boundaries.</param>
        /// <param name="binPositions">The bin positions.</param>
        /// <param name="binWidth">Width of the bin.</param>
        /// <returns>
        /// The binned data.
        /// </returns>
        public static int[] Bin(
            this IEnumerable<double[]> data,
            int numberOfBins,
            double min,
            double max,
            out double[] binBoundaries,
            out int[] binPositions,
            out double binWidth)
        {
            if (data != null)
            {
                return data.Flatten().Bin(numberOfBins, min, max, out binBoundaries, out binPositions, out binWidth);
            }

            binBoundaries = null;
            binPositions = null;
            binWidth = double.NaN;
            return null;
        }

        /// <summary>
        /// Bins the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="numberOfBins">The number of bins.</param>
        /// <param name="min">The min.</param>
        /// <param name="max">The max.</param>
        /// <returns>The binned data.</returns>
        public static int[] Bin(this double[] data, int numberOfBins, double min, double max)
        {
            double[] binBoundaries;
            int[] binPositions;
            double binWidth;
            return data.Bin(numberOfBins, min, max, out binBoundaries, out binPositions, out binWidth);
        }

        /// <summary>
        /// Bins the data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="numberOfBins">The number of bins.</param>
        /// <param name="binBoundaries">The bin boundaries.</param>
        /// <param name="binPositions">The bin positions.</param>
        /// <param name="binWidth">Width of the bin.</param>
        /// <returns>
        /// The binned data.
        /// </returns>
        public static int[] Bin(this IList<double> data, int numberOfBins, out double[] binBoundaries, out int[] binPositions, out double binWidth)
        {
            if (data != null)
            {
                return data.Bin(numberOfBins, data.Min(), data.Max(), out binBoundaries, out binPositions, out binWidth);
            }

            binBoundaries = null;
            binPositions = null;
            binWidth = double.NaN;
            return null;
        }

        /// <summary>
        /// Bins the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="numberOfBins">The number of bins.</param>
        /// <returns>The binned data.</returns>
        public static int[] Bin(this IList<double> data, int numberOfBins)
        {
            double[] binBoundaries;
            int[] binPositions;
            double binWidth;
            return data.Bin(numberOfBins, out binBoundaries, out binPositions, out binWidth);
        }

        /// <summary>
        /// Bins the data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="numberOfBins">The number of bins.</param>
        /// <param name="min">The min.</param>
        /// <param name="max">The max.</param>
        /// <param name="binBoundaries">The bin boundaries.</param>
        /// <param name="binPositions">The bin positions.</param>
        /// <param name="binWidth">Width of the bin.</param>
        /// <returns>
        /// The binned data.
        /// </returns>
        public static int[] Bin(
            this IList<double> data,
            int numberOfBins,
            double min,
            double max,
            out double[] binBoundaries,
            out int[] binPositions,
            out double binWidth)
        {
            if (data == null)
            {
                binBoundaries = null;
                binPositions = null;
                binWidth = double.NaN;
                return null;
            }

            int[] frequencies = new int[numberOfBins];
            binPositions = new int[data.Count];
            binWidth = (max - min) / numberOfBins;

            for (int i = 0; i < data.Count; i++)
            {
                double q = (data[i] - min) / binWidth;
                binPositions[i] = (int)q;
                if (binPositions[i] > 0 && binPositions[i] < numberOfBins)
                {
                    frequencies[binPositions[i]] += 1;
                }
            }

            double width = binWidth;
            binBoundaries = Enumerable.Range(0, numberOfBins + 1).Select((ia, i) => min + (i * width)).ToArray();

            return frequencies;
        }

        /// <summary>
        /// Gets the number of columns.
        /// </summary>
        /// <typeparam name="T">The array type.</typeparam>
        /// <param name="jaggedArray">The jagged array.</param>
        /// <returns>
        /// The number of columns.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">jagged Array</exception>
        public static int GetNumberOfColumns<T>(this IEnumerable<T[]> jaggedArray)
        {
            if (jaggedArray == null)
            {
                throw new ArgumentNullException("jaggedArray");
            }

            return jaggedArray.Select(row => row.Length).Max();
        }

        /// <summary>
        /// Transposes the specified 2D array.
        /// </summary>
        /// <typeparam name="T">The array type.</typeparam>
        /// <param name="array2D">The array2D.</param>
        /// <returns>The transposed array.</returns>
        public static T[,] Transpose<T>(this T[,] array2D)
        {
            if (array2D == null)
            {
                return null;
            }

            int rows = array2D.GetLength(0);
            int cols = array2D.GetLength(1);
            T[,] transposed = new T[cols, rows];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    transposed[j, i] = array2D[i, j];
                }
            }

            return transposed;
        }

        /// <summary>
        /// Transposes the specified jagged array.
        /// </summary>
        /// <typeparam name="T">The array type.</typeparam>
        /// <param name="jaggedArray">The jagged array.</param>
        /// <returns>The transposed array.</returns>
        public static T[][] Transpose<T>(this T[][] jaggedArray)
        {
            if (jaggedArray == null)
            {
                return null;
            }

            int rows = jaggedArray.Length;
            int cols = GetNumberOfColumns(jaggedArray);

            T[][] transposed = new T[cols][];

            for (int i = 0; i < rows; i++)
            {
                T[] row = jaggedArray[i];

                if (row == null)
                {
                    throw new ArgumentException("Array should be 2 dimensional");
                }

                for (int j = 0; j < cols; j++)
                {
                    // Initialise the inner array
                    if (i == 0)
                    {
                        transposed[j] = new T[rows];
                    }
                    else
                    {
                        if (row.Length != transposed.Length)
                        {
                            throw new NotSupportedException("Only rectangular arrays are supported");
                        }
                    }

                    // Set the value
                    transposed[j][i] = row[j];
                }
            }

            return transposed;
        }

        /// <summary>
        /// The to jagged array.
        /// </summary>
        /// <param name="matrix">
        /// The matrix.
        /// </param>
        /// <returns>
        /// The jagged array
        /// </returns>
        public static IEnumerable<double[]> ToJaggedArray(this Matrix matrix)
        {
            return matrix.ToArray().ToJaggedArray();
        }

        /// <summary>
        /// The to jagged array.
        /// </summary>
        /// <typeparam name="T">The type of the 2D array</typeparam>
        /// <param name="matrix">The matrix.</param>
        /// <returns>
        /// The jagged array
        /// </returns>
        public static T[][] ToJaggedArray<T>(this T[,] matrix)
        {
            if (matrix == null)
            {
                return null;
            }

            T[][] darray = new T[matrix.GetLength(0)][];
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                darray[i] = new T[matrix.GetLength(1)];
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    darray[i][j] = matrix[i, j];
                }
            }

            return darray;
        }

        /// <summary>
        /// Converts jagged array to 2D array, using default(T) for missing values
        /// </summary>
        /// <typeparam name="T">The type of the jagged array</typeparam>
        /// <param name="jaggedArray">The jagged array.</param>
        /// <returns>The 2D array</returns>
        public static T[,] To2DArray<T>(this T[][] jaggedArray)
        {
            if (jaggedArray == null)
            {
                return null;
            }

            int cols = jaggedArray.GetNumberOfColumns();

            T[,] darray = new T[jaggedArray.Length, cols];

            for (int i = 0; i < jaggedArray.Length; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (j < jaggedArray[i].Length)
                    {
                        darray[i, j] = jaggedArray[i][j];
                    }
                    else
                    {
                        darray[i, j] = default(T);
                    }
                }
            }

            return darray;
        }

        /// <summary>
        /// Converts a jagged array to a matrix.
        /// </summary>
        /// <param name="jaggedArray">The jagged array.</param>
        /// <returns>
        /// "The Matrix."
        /// </returns>
        public static Matrix ToMatrix(this double[][] jaggedArray)
        {
            return new Matrix(jaggedArray.Length, jaggedArray.GetNumberOfColumns(), jaggedArray.Flatten());
        }

        /// <summary>
        /// Converts a jagged array to a matrix.
        /// </summary>
        /// <typeparam name="T">The type of the array</typeparam>
        /// <param name="jaggedArray">The jagged array.</param>
        /// <returns>
        /// "The Matrix."
        /// </returns>
        public static Matrix ToMatrix<T>(this T[][] jaggedArray)
        {
            // TODO: Make this more efficient, and add some exception handling
            return new Matrix(
                jaggedArray.Length,
                jaggedArray.GetNumberOfColumns(),
                jaggedArray.Flatten().Select(ia => Convert.ToDouble(ia)).ToArray());
        }

        /// <summary>
        /// Gets the means.
        /// </summary>
        /// <param name="variables">The variables.</param>
        /// <returns>The means</returns>
        public static double[] GetMeans(this IEnumerable<Bernoulli> variables)
        {
            return variables.Select(ia => ia.GetMean()).ToArray();
        }

        /// <summary>
        /// Gets the means.
        /// </summary>
        /// <param name="jaggedArray">The jagged array.</param>
        /// <returns>The means.</returns>
        public static double[][] GetMeans(this IEnumerable<IEnumerable<Bernoulli>> jaggedArray)
        {
            return jaggedArray.Select(inner => inner.GetMeans()).ToArray();
        }

        /// <summary>
        /// Gets the means.
        /// </summary>
        /// <param name="variables">The variables.</param>
        /// <returns>The means.</returns>
        public static double[] GetMeans(this IEnumerable<Beta> variables)
        {
            return variables.Select(ia => ia.GetMean()).ToArray();
        }

        /// <summary>
        /// Gets the means.
        /// </summary>
        /// <param name="jaggedArray">The jagged array.</param>
        /// <returns>The means.</returns>
        public static double[][] GetMeans(this IEnumerable<IEnumerable<Beta>> jaggedArray)
        {
            return jaggedArray.Select(inner => inner.GetMeans()).ToArray();
        }

        /// <summary>
        /// Gets the log probability of truth.
        /// </summary>
        /// <param name="variables">The variables.</param>
        /// <param name="truth">The truth.</param>
        /// <returns>The log probabilities.</returns>
        public static double[] GetLogProbabilityOfTruth(this IList<Bernoulli> variables, IList<bool> truth)
        {
            if (variables == null || truth == null)
            {
                return null;
            }

            if (variables.Count != truth.Count)
            {
                throw new ArgumentException("variables and truth should be the same length");
            }

            return variables.Select((ia, i) => ia.GetLogProb(truth[i])).ToArray();
        }

        /// <summary>
        /// Gets the log probability of truth.
        /// </summary>
        /// <param name="jaggedArray">The jagged array.</param>
        /// <param name="truth">The truth.</param>
        /// <returns>The log probabilities.</returns>
        public static double[][] GetLogProbabilityOfTruth(this Bernoulli[][] jaggedArray, bool[][] truth)
        {
            if (jaggedArray == null || truth == null)
            {
                return null;
            }

            if (jaggedArray.Length != truth.Length)
            {
                throw new ArgumentException("variables and truth should be the same length");
            }

            return jaggedArray.Select((inner, i) => inner.GetLogProbabilityOfTruth(truth[i])).ToArray();
        }

        /// <summary>
        /// Boolean to double.
        /// </summary>
        /// <param name="variables">The variables.</param>
        /// <returns>The doubles.</returns>
        public static IEnumerable<double> BooleanToDouble(this IEnumerable<bool> variables)
        {
            return variables.Select(Convert.ToDouble);
        }

        /// <summary>
        /// Cumulative sum.
        /// </summary>
        /// <param name="sequence">The sequence.</param>
        /// <returns>The cumulative sum.</returns>
        public static IEnumerable<double> CumulativeSum(this IEnumerable<double> sequence)
        {
            double sum = 0.0;
            foreach (var d in sequence)
            {
                sum += d;
                yield return sum;
            }
        }

        /// <summary>
        /// Cumulative average.
        /// </summary>
        /// <param name="sequence">The sequence.</param>
        /// <returns>The cumulative average.</returns>
        public static IEnumerable<double> CumulativeAverage(this IEnumerable<double> sequence)
        {
            double sum = 0.0;
            int length = 0;
            foreach (var d in sequence)
            {
                sum += d;
                length += 1;
                yield return sum / length;
            }
        }

        /// <summary>
        /// Sums the specified array.
        /// </summary>
        /// <typeparam name="T">The array type.</typeparam>
        /// <param name="array">The array.</param>
        /// <returns>
        /// The sum.
        /// </returns>
        public static double Sum<T>(this IEnumerable<T> array)
        {
            if (array == null)
            {
                return double.NaN;
            }

            if (array is IEnumerable<bool>)
            {
                return array.Count(c => (bool)(object)c);
            }

            return array.Sum(ia => Convert.ToDouble(ia));
        }

        /// <summary>
        /// Averages the specified array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <returns>The average.</returns>
        public static double Average(this IList<bool> array)
        {
            return array == null ? double.NaN : array.Sum() / array.Count;
        }

        /// <summary>
        /// Averages the specified array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <returns>The average.</returns>
        public static double Average(this IEnumerable<bool> array)
        {
            return array == null ? double.NaN : array.Average(ia => ia ? 1.0 : 0.0);
        }

        /// <summary>
        /// Sums the specified jagged array.
        /// </summary>
        /// <typeparam name="T">The array type.</typeparam>
        /// <param name="jaggedArray">The jagged array.</param>
        /// <param name="dimension">The dimension.</param>
        /// <returns>
        /// The sums along the specified dimension.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">dimension should be 0 or 1</exception>
        public static double[] Sum<T>(this T[][] jaggedArray, int dimension = 0)
        {
            if (jaggedArray == null)
            {
                return null;
            }

            if (dimension < 0 || dimension > 1)
            {
                throw new ArgumentOutOfRangeException("dimension");
            }

            return dimension == 0 ? jaggedArray.Select(ia => ia.Sum()).ToArray() : jaggedArray.Transpose().Sum();
        }

        /// <summary>
        /// Averages the specified jagged array.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="jaggedArray">The jagged array.</param>
        /// <param name="dimension">The dimension.</param>
        /// <returns>The averages along the specified dimension.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">dimension should be 0 or 1.</exception>
        public static double[] Average<T>(this T[][] jaggedArray, int dimension = 0)
        {
            while (true)
            {
                if (jaggedArray == null)
                {
                    return null;
                }

                if (dimension < 0 || dimension > 1)
                {
                    throw new ArgumentOutOfRangeException("dimension");
                }

                if (dimension == 0)
                {
                    return jaggedArray.Select(ia => ia.Select(inner => Convert.ToDouble(inner)).Average()).ToArray();
                }

                jaggedArray = jaggedArray.Transpose();
                dimension = 0;
            }
        }

        /// <summary>
        /// To the dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="keyValuePairs">The key value pairs.</param>
        /// <returns>The <see cref="Dictionary{TKey, TValue}"/></returns>
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
        {
            return keyValuePairs.ToDictionary(ia => ia.Key, ia => ia.Value);
        }

        /// <summary>
        /// Mean squared error.
        /// </summary>
        /// <typeparam name="T">Target type.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <returns>
        /// The mean squared error between the source and target arrays.
        /// </returns>
        public static double MeanSquaredError<T>(this IEnumerable<double> source, IEnumerable<T> target)
        {
            return source.ToArray().MeanSquaredEror(target.ToArray());
        }

        /// <summary>
        /// Mean squared error.
        /// </summary>
        /// <typeparam name="T">The type of the target.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <returns>The mean squared error.</returns>
        public static double MeanSquaredEror<T>(this double[] source, T[] target)
        {
            return Math.Pow(source.NormDifference(target, 2), 2) / source.Length;
        }

        /// <summary>
        /// Means squared error.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <param name="dimension">The dimension.</param>
        /// <returns>The means along the specified dimension.</returns>
        public static double[] MeanSquaredError<T>(this double[][] source, T[][] target, int dimension = 0)
        {
            if (source == null || target == null)
            {
                return null;
            }

            int length = (dimension == 0) ? GetNumberOfColumns(source) : source.Length;
            return source.NormDifference(target, 2, dimension).Select(ia => Math.Pow(ia, 2) / length).ToArray();
        }

        /// <summary>
        /// Norm difference between two arrays.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <param name="norm">The norm.</param>
        /// <returns>The norm difference.</returns>
        /// <exception cref="System.ArgumentException">source and target arrays are not the same length</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">norm should be 1 or 2</exception>
        public static double NormDifference<T>(this double[] source, T[] target, int norm)
        {
            if (source == null || target == null)
            {
                return double.NaN;
            }

            if (source.Length != target.Length)
            {
                throw new ArgumentException("source and target arrays are not the same length");
            }

            if (norm < 1 || norm > 2)
            {
                throw new ArgumentOutOfRangeException("norm");
            }

            double normDiff = source.Select((t, i) => Math.Pow(t - Convert.ToDouble(target[i]), norm)).Sum();

            return norm == 2 ? Math.Sqrt(normDiff) : normDiff;
        }

        /// <summary>
        /// Norm difference between two jagged arrays.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <param name="norm">The norm.</param>
        /// <param name="dimension">The dimension.</param>
        /// <returns>The norm differences along the specified dimension.</returns>
        /// <exception cref="System.ArgumentException">source and target arrays are not the same length</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// norm
        /// or
        /// dimension
        /// </exception>
        public static IEnumerable<double> NormDifference<T>(this double[][] source, T[][] target, int norm, int dimension = 0)
        {
            while (true)
            {
                if (source == null || target == null)
                {
                    return null;
                }

                if (source.Length != target.Length)
                {
                    throw new ArgumentException("source and target arrays are not the same length");
                }

                if (norm < 1 || norm > 2)
                {
                    throw new ArgumentOutOfRangeException("norm");
                }

                if (dimension < 0 || dimension > 1)
                {
                    throw new ArgumentOutOfRangeException("dimension");
                }

                if (dimension == 0)
                {
                    return source.Select((ia, i) => ia.NormDifference(target[i], norm)).ToArray();
                }

                source = source.Transpose();
                target = target.Transpose();
                dimension = 0;
            }
        }

        /// <summary>
        /// Norm of the specified source enumerable.
        /// </summary>
        /// <typeparam name="T">The type of the source.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="norm">The norm.</param>
        /// <returns>
        /// The norm of the source.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">norm should be 1 or 2</exception>
        public static double Norm<T>(this IEnumerable<T> source, int norm)
        {
            if (source == null)
            {
                return double.NaN;
            }

            if (norm < 1 || norm > 2)
            {
                throw new ArgumentOutOfRangeException("norm");
            }

            double result = source.Select(ia => Math.Pow(Convert.ToDouble(ia), norm)).Sum();
            return norm == 2 ? Math.Sqrt(result) : result;
        }

        /// <summary>
        /// Splits camel case string. CamelCaseString will become Camel Case String
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>The string with spaces.</returns>
        public static string SplitCamelCase(this string input)
        {
            int i = 0;

            var splitString =
                input.Select(
                    c =>
                    new
                        {
                            Character = new string(c, 1),
                            Start =
                        i++ == 0
                        || (char.IsUpper(input[i - 1]) && (!char.IsUpper(input[i - 2]) || (i < input.Length && !char.IsUpper(input[i]))))
                        });
            var splitStringWithSpaces = splitString.Select(x => x.Start ? " " + x.Character : x.Character);
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(string.Join(string.Empty, splitStringWithSpaces)).Trim();
        }

        #region Gaussian Distribution

        /// <summary>
        /// Cumulative density function using ERF approximation.
        /// See http://en.wikipedia.org/wiki/Normal_distribution#Numerical_approximations_for_the_normal_CDF
        /// </summary>
        /// <param name="gaussian">The gaussian.</param>
        /// <param name="x">The x.</param>
        /// <returns>CDF (x)</returns>
        public static double CumulativeDistributionFunction(this Gaussian gaussian, double x)
        {
            if (x <= double.MinValue)
            {
                return 0.0;
            }

            if (x >= double.MaxValue)
            {
                return 1.0;
            }

            // ERF approximation
            return 0.5 * (1 + Erf((x - gaussian.GetMean()) / (Math.Sqrt(2) * Math.Sqrt(gaussian.GetVariance()))));
        }

        /// <summary>
        /// Error function.
        /// See <a href="http://en.wikipedia.org/wiki/Error_function#Numerical_approximation" />
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>ERF (x).</returns>
        public static double Erf(double x)
        {
            double t = 1.0 / (1.0 + (0.5 * Math.Abs(x)));

            double tau = t * Math.Exp(-Math.Pow(x, 2) + ErfCoefficients.Select((c, i) => c * Math.Pow(t, i)).Sum());

            return x >= 0 ? 1 - tau : tau - 1;
        }

        /// <summary>
        /// Quantile function (inverse cumulative density function) of the specified distribution.
        /// Calculated for +/- 4 standard deviations
        /// </summary>
        /// <param name="gaussian">The distribution.</param>
        /// <param name="y">The y.</param>
        /// <param name="steps">The steps.</param>
        /// <returns>
        /// The <see cref="double" />.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">y should be in the interval [0, 1]</exception>
        public static double InverseCumulativeDistributionFunction(this Gaussian gaussian, double y, int steps = 1001)
        {
            if (y < 0.0 || y > 1.0)
            {
                throw new ArgumentOutOfRangeException("y");
            }

            double xmin = gaussian.GetMean() - (4 * Math.Sqrt(gaussian.GetVariance()));
            double xmax = gaussian.GetMean() + (4 * Math.Sqrt(gaussian.GetVariance()));
            double delta = (xmax - xmin) / (steps - 1);
            double lastx = double.NegativeInfinity;

            for (int i = 0; i < steps; i++)
            {
                double x = xmin + (delta * i);

                if (gaussian.CumulativeDistributionFunction(x) > y)
                {
                    return lastx;
                }

                lastx = x;
            }

            return double.PositiveInfinity;
        }

        #endregion

        /// <summary>
        /// Calculate integral with trapezoidal rule using Simpson's rule
        /// See <a href="http://en.wikipedia.org/wiki/Simpson's_rule" />
        /// </summary>
        /// <param name="f">The function.</param>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="intervals">The intervals.</param>
        /// <returns>
        /// The area under the curve
        /// </returns>
        public static double Integrate(this Func<double, double> f, double min, double max, int intervals)
        {
            double h = (max - min) / intervals;
            double s = f(min) + f(max);

            for (int i = 1; i < intervals; i++)
            {
                s += (i % 2 == 0) ? 2 * f(min + (i * h)) : 4 * f(min + (i * h));
            }

            return s * h / 3;
        }

        /// <summary>
        /// Integrates the specified function using the Trapezoid rule
        /// </summary>
        /// <param name="f">The f.</param>
        /// <param name="x">The x.</param>
        /// <returns>The area under the curve</returns>
        public static double Integrate(this Func<double, double> f, double[] x)
        {
            double s = 0.0;

            for (int i = 0; i < x.Length - 1; i++)
            {
                s += (x[i + 1] - x[i]) * (f(x[i]) + f(x[i + 1]));
            }

            return s / 2.0;
        }

        /// <summary>
        /// Integrates the specified points using the Trapezoid rule.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>The area under the curve</returns>
        public static double Integrate(this IEnumerable<Point> points)
        {
            var pdict = points.GroupBy(g => g.X).OrderBy(g => g.Key).ToDictionary(p => p.First().X, p => p.First().Y);
            Func<double, double> f = x => pdict[x];

            return f.Integrate(pdict.Keys.ToArray());
        }

        /// <summary>
        /// Integrates the specified function using the Trapezoid rule.
        /// </summary>
        /// <param name="f">The f.</param>
        /// <param name="range">The range.</param>
        /// <returns>The area under the curve.</returns>
        public static double Integrate(this Func<double, double> f, RealRange range)
        {
            return f.Integrate(range.Values.ToArray());
        }
    }
}
