// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Research.Glo.Views;

    using MBMLViews;

    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Models;

#if NETFULL
    using Point = System.Windows.Point;
#else
    using Point = MBMLCommon.Point;
#endif

    /// <summary>
    /// The utils. Convenience methods related to Gaussians, function evaluation, and other 
    /// miscellaneous tasks. 
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Gets the standard gaussian.
        /// </summary>
        public static Gaussian StandardGaussian
        {
            get
            {
                return new Gaussian(0, 1);
            }
        }

        /// <summary>
        /// Conservative skill estimate.
        /// </summary>
        /// <param name="gaussian">The gaussian.</param>
        /// <returns>The <see cref="double"/>.</returns>
        public static double ConservativeSkill(Gaussian gaussian)
        {
            return ConservativeSkill(gaussian.GetMean(), gaussian.GetVariance());
        }

        /// <summary>
        /// The conservative skill estimate.
        /// </summary>
        /// <param name="mean">The mean.</param>
        /// <param name="variance">The variance.</param>
        /// <returns>
        /// The <see cref="double" />.
        /// </returns>
        public static double ConservativeSkill(double mean, double variance)
        {
            return mean - Math.Sqrt(variance * 3.0);
        }

        /// <summary>
        /// Conservative skill estimate.
        /// </summary>
        /// <typeparam name="TSkills">The type of the skills.</typeparam>
        /// <param name="skill">The skill.</param>
        /// <returns>
        /// The <see cref="double" />.
        /// </returns>
        /// <exception cref="System.NotSupportedException">Unsupported skill type</exception>
        public static double ConservativeSkill<TSkills>(TSkills skill)
        {
            var g = skill as Gaussian?;
            if (g.HasValue)
            {
                return ConservativeSkill(g.Value);
            }

            var d = skill as double?;
            if (d.HasValue)
            {
                return ConservativeSkill(d.Value, 0);
            }

            throw new NotSupportedException("Unsupported skill type");
        }

        /// <summary>
        /// Gets the mean and standard deviation.
        /// </summary>
        /// <param name="gaussian">The gaussian.</param>
        /// <param name="index">The index.</param>
        /// <returns>The <see cref="GaussianPoint"/>.</returns>
        public static GaussianPoint GetMeanAndStandardDeviation(Gaussian gaussian, int index)
        {
            return new GaussianPoint(index, gaussian, ia => Math.Sqrt(ia.GetVariance()));
        }

        /// <summary>
        /// Gets the mean and three sigma.
        /// </summary>
        /// <param name="gaussian">The gaussian.</param>
        /// <param name="index">The index.</param>
        /// <returns>The <see cref="GaussianPoint"/>.</returns>
        public static GaussianPoint GetMeanAndThreeSigma(Gaussian gaussian, int index)
        {
            return new GaussianPoint(index, gaussian, ia => 3 * Math.Sqrt(ia.GetVariance()));
        }

        /// <summary>
        /// Gets the gaussian point.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="index">The index.</param>
        /// <returns>The <see cref="GaussianPoint"/>.</returns>
        public static GaussianPoint GetGaussianPoint(double value, int index = 0)
        {
            return new GaussianPoint(index, Gaussian.PointMass(value), g => 0.0);
        }

        /// <summary>
        /// Gets the mean and variance. Note the index parameter is only for a consistent signature and isn't used
        /// </summary>
        /// <param name="gaussian">The gaussian.</param>
        /// <param name="index">The index.</param>
        /// <returns>
        /// The <see cref="GaussianPoint" />.
        /// </returns>
        public static double GetMean(Gaussian gaussian, int index = 0)
        {
            return gaussian.GetMean();
        }

        /// <summary>
        /// Evaluates the specified function.
        /// </summary>
        /// <param name="func">The function.</param>
        /// <param name="range">The range.</param>
        /// <returns>The points representing the function evaluated over the given range.</returns>
        public static Point[] Evaluate(Func<double, double> func, RealRange range)
        {
            return range.Values.Select(x => new Point(x, func(x))).ToArray();
        }

        /// <summary>
        /// Evaluates the specified function.
        /// </summary>
        /// <param name="func">The function.</param>
        /// <param name="upperFunc">The upper function.</param>
        /// <param name="lowerFunc">The lower function.</param>
        /// <param name="range">The range.</param>
        /// <returns>
        /// The points representing the function evaluated over the given range.
        /// </returns>
        public static PointWithBounds[] Evaluate(
            Func<double, double> func,
            Func<double, double> upperFunc,
            Func<double, double> lowerFunc,
            RealRange range)
        {
            return range.Values.Select(x => new PointWithBounds(x, func(x), lowerFunc(x), upperFunc(x))).ToArray();
        }

        /// <summary>
        /// Evaluates the specified gaussian.
        /// </summary>
        /// <param name="gaussian">The gaussian.</param>
        /// <param name="range">The range.</param>
        /// <returns>
        /// The points representing the function evaluated over the given range.
        /// </returns>
        public static Point[] Evaluate(Gaussian gaussian, RealRange range)
        {
            return range.Values.Select(x => new Point(x, Math.Exp(gaussian.GetLogProb(x)))).ToArray();
        }
        
        /// <summary>
        /// Gets the range.
        /// </summary>
        /// <param name="gaussians">The gaussians.</param>
        /// <returns>The <see cref="RealRange"/>.</returns>
        public static RealRange GetRange(IList<Gaussian> gaussians)
        {
            double min = double.PositiveInfinity;
            double max = double.NegativeInfinity;

            // loop over gaussians to get x range, using 4x sigma on each side
            foreach (var gaussian in gaussians)
            {
                min = Math.Min(min, -(4 * Math.Sqrt(gaussian.GetVariance())) + gaussian.GetMean());
                max = Math.Max(max, (4 * Math.Sqrt(gaussian.GetVariance())) + gaussian.GetMean());
            }

            int interpolants = gaussians.Count > 10 ? 200 : 1000;

            return new RealRange { Min = min, Max = max, Steps = interpolants };
        }

        /// <summary>
        /// Evaluations of the average of a list of Gaussian distributions.
        /// </summary>
        /// <param name="gaussians">The gaussians.</param>
        /// <returns>The <see cref="Point"/> array.</returns>
        public static Point[] GaussianAverage(IList<Gaussian> gaussians)
        {
            return GaussianAverage(gaussians, GetRange(gaussians));
        }

        /// <summary>
        /// Evaluations of the average of a list of Gaussian distributions.
        /// </summary>
        /// <param name="gaussians">The gaussians.</param>
        /// <param name="range">The range.</param>
        /// <returns>The <see cref="Point"/> array.</returns>
        public static Point[] GaussianAverage(IEnumerable<Gaussian> gaussians, RealRange range)
        {
            return range.Values.Select(x => new Point(x, gaussians.Average(gaussian => Math.Exp(gaussian.GetLogProb(x))))).ToArray();
        }

        /// <summary>
        /// Argument of the maximum.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <typeparam name="TArg">The type of the argument.</typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="converter">The converter.</param>
        /// <returns>The argument of the maximum.</returns>
        /// <exception cref="System.InvalidOperationException">Sequence has no elements.</exception>
        public static TSource ArgMax<TSource, TArg>(this IEnumerable<TSource> enumerable, Converter<TSource, TArg> converter)
            where TArg : IComparable<TArg>
        {
            var enumerator = enumerable.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                throw new InvalidOperationException("Sequence has no elements.");
            }

            var current = enumerator.Current;
            if (!enumerator.MoveNext())
            {
                return current;
            }

            var maxVal = converter(current);
            do
            {
                TSource comparator;
                TArg value;
                if ((value = converter(comparator = enumerator.Current)).CompareTo(maxVal) <= 0)
                {
                    continue;
                }

                current = comparator;
                maxVal = value;
            }
            while (enumerator.MoveNext());
            return current;
        }
        
        /////// <summary>
        /////// Evaluates and adds a Gaussian to the collection.
        /////// </summary>
        /////// <param name="collection">The collection.</param>
        /////// <param name="title">The title.</param>
        /////// <param name="gaussian">The gaussian.</param>
        /////// <param name="range">The range.</param>
        ////internal static void EvaluateAndAddToDictionary(
        ////    this Dictionary collection, 
        ////    string title, 
        ////    Gaussian gaussian, 
        ////    RealRange range)
        ////{
        ////    collection.Add(string.Format("{0} = {1}", title, gaussian.ToString("N2")), Evaluate(gaussian, range));
        ////}

        /// <summary>
        /// Gets the default engine.
        /// </summary>
        /// <param name="showFactorGraph">if set to <c>true</c> [show factor graph].</param>
        /// <returns>The <see cref="InferenceEngine"/>. </returns>
        internal static InferenceEngine GetDefaultEngine(bool showFactorGraph)
        {
            return new InferenceEngine
                       {
                           ShowProgress = false,
                           ShowSchedule = false,
                           ShowTimings = false,
                           ShowMsl = false,
                           ShowFactorGraph = showFactorGraph,
                           ShowWarnings = false,
                           NumberOfIterations = 10,
                       };
        }
    }
}