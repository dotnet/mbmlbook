// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLViews
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Math;

    /// <summary>
    /// The distribution array helpers.
    /// </summary>
    public static class DistributionArrayHelpers
    {
        /// <summary>
        /// Create a distribution array.
        /// </summary>
        /// <typeparam name="TDistribution">The type of the distribution.</typeparam>
        /// <param name="distributions">The distributions.</param>
        /// <returns>
        /// The DistributionStructArray{TDistribution, double}
        /// </returns>
        public static DistributionStructArray<TDistribution, double> DistributionArray<TDistribution>(IEnumerable<TDistribution> distributions)
            where TDistribution : struct, IDistribution<double>, SettableToProduct<TDistribution>, SettableToRatio<TDistribution>,
                SettableToPower<TDistribution>, SettableToWeightedSum<TDistribution>, CanGetLogAverageOf<TDistribution>,
                CanGetLogAverageOfPower<TDistribution>, CanGetAverageLog<TDistribution>, Sampleable<double>, SettableToUniform
        {
            return distributions == null ? null : (DistributionStructArray<TDistribution, double>)Distribution<double>.Array(distributions.ToArray());
        }

        /// <summary>
        /// Copy the distribution array.
        /// </summary>
        /// <typeparam name="TDistribution">The type of the distribution.</typeparam>
        /// <param name="arrayToCopy">The array to copy.</param>
        /// <returns>
        /// The <see cref="DistributionStructArray{TDistribution, Double}" />.
        /// </returns>
        public static DistributionStructArray<TDistribution, double> Copy<TDistribution>(IEnumerable<TDistribution> arrayToCopy)
            where TDistribution : struct, IDistribution<double>, SettableToProduct<TDistribution>, SettableToRatio<TDistribution>,
                SettableToPower<TDistribution>, SettableToWeightedSum<TDistribution>, CanGetLogAverageOf<TDistribution>,
                CanGetLogAverageOfPower<TDistribution>, CanGetAverageLog<TDistribution>, Sampleable<double>
        {
            return (DistributionStructArray<TDistribution, double>)Distribution<double>.Array(arrayToCopy.ToArray());
        }

        /// <summary>
        /// Copy the distribution array.
        /// </summary>
        /// <typeparam name="TDistribution">The type of the distribution.</typeparam>
        /// <param name="arrayToCopy">The array to copy.</param>
        /// <returns>
        /// The <see cref="DistributionStructArray{TDistribution, Double}" /> array.
        /// </returns>
        public static DistributionStructArray<TDistribution, double>[] Copy<TDistribution>(IEnumerable<IList<TDistribution>> arrayToCopy)
            where TDistribution : struct, IDistribution<double>, SettableToProduct<TDistribution>, SettableToRatio<TDistribution>,
                SettableToPower<TDistribution>, SettableToWeightedSum<TDistribution>, CanGetLogAverageOf<TDistribution>,
                CanGetLogAverageOfPower<TDistribution>, CanGetAverageLog<TDistribution>, Sampleable<double>
        {
            return arrayToCopy.Select(Copy).ToArray();
        }

        /// <summary>
        /// The uniform gaussian array.
        /// </summary>
        /// <typeparam name="TDistribution">The type of the distribution.</typeparam>
        /// <param name="count">The count.</param>
        /// <returns>
        /// The array of distributions.
        /// </returns>
        public static DistributionStructArray<TDistribution, double> UniformArray<TDistribution>(int count)
            where TDistribution : struct, IDistribution<double>, SettableToProduct<TDistribution>, SettableToRatio<TDistribution>,
                SettableToPower<TDistribution>, SettableToWeightedSum<TDistribution>, CanGetLogAverageOf<TDistribution>,
                CanGetLogAverageOfPower<TDistribution>, CanGetAverageLog<TDistribution>, Sampleable<double>, SettableToUniform
        {
            Func<TDistribution, TDistribution> uniform = x =>
                {
                    x.SetToUniform();
                    return x;
                };

            return
                (DistributionStructArray<TDistribution, double>)
                Distribution<double>.Array(
                    Enumerable.Repeat(uniform((TDistribution)Activator.CreateInstance(typeof(TDistribution))), count).ToArray());
        }

        /// <summary>
        /// Creates the gaussian array.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <param name="mean">The mean.</param>
        /// <param name="variance">The variance.</param>
        /// <returns>The <see cref="DistributionStructArray{Gaussian, Double}" /> array.</returns>
        public static DistributionStructArray<Gaussian, double> CreateGaussianArray(int count, double mean, double variance)
        {
            return
                (DistributionStructArray<Gaussian, double>)
                Distribution<double>.Array(Enumerable.Repeat(Gaussian.FromMeanAndVariance(mean, variance), count).ToArray());
        }

        /// <summary>
        /// Creates the gamma array.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <param name="shape">The shape.</param>
        /// <param name="scale">The scale.</param>
        /// <returns>The <see cref="DistributionStructArray{Gamma, Double}" /> array.</returns>
        public static DistributionStructArray<Gamma, double> CreateGammaArray(int count, double shape, double scale)
        {
            return
                (DistributionStructArray<Gamma, double>)
                Distribution<double>.Array(Enumerable.Repeat(Gamma.FromShapeAndScale(shape, scale), count).ToArray());
        }

        /// <summary>
        /// Repeats the specified distribution.
        /// </summary>
        /// <typeparam name="TDistribution">The type of the distribution.</typeparam>
        /// <param name="distribution">The distribution.</param>
        /// <param name="count">The count.</param>
        /// <returns>The <see cref="DistributionStructArray{TDistribution, Double}" /> array.</returns>
        public static DistributionStructArray<TDistribution, double> Repeat<TDistribution>(TDistribution distribution, int count)
            where TDistribution : struct, IDistribution<double>, SettableToProduct<TDistribution>, SettableToRatio<TDistribution>,
                SettableToPower<TDistribution>, SettableToWeightedSum<TDistribution>, CanGetLogAverageOf<TDistribution>,
                CanGetLogAverageOfPower<TDistribution>, CanGetAverageLog<TDistribution>, Sampleable<double>, SettableToUniform
        {
            return (DistributionStructArray<TDistribution, double>)Distribution<double>.Array(Enumerable.Repeat(distribution, count).ToArray());
        }
    }
}