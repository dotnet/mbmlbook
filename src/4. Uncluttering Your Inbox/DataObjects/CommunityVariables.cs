// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using UnclutteringYourInbox.Features;

    using Microsoft.Research.Glo.Views;

    using Microsoft.ML.Probabilistic.Distributions;

    /// <summary>
    /// The variables (priors/posteriors).
    /// </summary>
    public abstract class CommunityVariables
    {
        /// <summary>
        /// Gets or sets the weight mean priors/posteriors.
        /// </summary>
        public Dictionary<FeatureBucket, Gaussian> WeightMeans { get; set; }

        /// <summary>
        /// Gets or sets the weight precision priors/posteriors.
        /// </summary>
        public Dictionary<FeatureBucket, Gamma> WeightPrecisions { get; set; }

        /// <summary>
        /// Gets or sets the threshold priors/posteriors.
        /// </summary>
        public Dictionary<string, Gaussian> Thresholds { get; set; }

        /// <summary>
        /// Gets or sets the posteriors.
        /// </summary>
        public Dictionary<string, Posteriors> Posteriors { get; set; }

        /// <summary>
        /// Gets all the means as a dictionary (for plotting on the same graph).
        /// </summary>
        public Dictionary<string, Gaussian> MeansDict
        {
            get
            {
                return this.WeightMeans == null 
                    ? null 
                    : this.WeightMeans.ToDictionary(ia => ia.Key.ToString(), ia => ia.Value);
            }
        }

        /// <summary>
        /// Gets all the precisions as a dictionary (for plotting on the same graph).
        /// </summary>
        public Dictionary<string, Gamma> PrecisionDict
        {
            get
            {
                return this.WeightPrecisions == null 
                    ? null 
                    : this.WeightPrecisions.ToDictionary(ia => ia.Key.ToString(), ia => ia.Value);
            }
        }
    }

    /// <summary>
    /// The priors.
    /// </summary>
    public class CommunityPriors : CommunityVariables
    {
        /// <summary>
        /// Gets or sets the noise prior variance
        /// </summary>
        public double NoiseVariance { get; set; }

        /// <summary>
        /// Generates the priors.
        /// </summary>
        /// <param name="buckets">The buckets.</param>
        /// <param name="precisionShape">The precision shape.</param>
        /// <param name="precisionScale">The precision scale.</param>
        /// <param name="thresholdAndNoiseVariance">The threshold and noise variance.</param>
        /// <param name="userNames">The user names.</param>
        /// <returns>The <see cref="CommunityPriors" />.</returns>
        internal static CommunityPriors Generate(
            ICollection<FeatureBucket> buckets,
            double precisionShape,
            double precisionScale,
            double thresholdAndNoiseVariance,
            IEnumerable<string> userNames)
        {
            return new CommunityPriors
                       {
                           WeightMeans = buckets.ToDictionary(ia => ia, ia => Gaussian.FromMeanAndVariance(0.0, 1.0)),
                           WeightPrecisions =
                               buckets.ToDictionary(ia => ia, ia => Gamma.FromShapeAndScale(precisionShape, precisionScale)),
                           Thresholds =
                               userNames.ToDictionary(
                                   ia => ia,
                                   ia => Gaussian.FromMeanAndVariance(0.0, thresholdAndNoiseVariance)),
                           NoiseVariance = thresholdAndNoiseVariance
                       };
        }

        /// <summary>
        /// Generates the priors.
        /// </summary>
        /// <param name="buckets">The buckets.</param>
        /// <param name="point">The point.</param>
        /// <param name="thresholdAndNoiseVariance">The threshold and noise variance.</param>
        /// <param name="userNames">The user names.</param>
        /// <returns>The <see cref="CommunityPriors" />.</returns>
        internal static CommunityPriors Generate(
            ICollection<FeatureBucket> buckets,
            double point,
            double thresholdAndNoiseVariance,
            IEnumerable<string> userNames)
        {
            return new CommunityPriors
            {
                WeightMeans = buckets.ToDictionary(ia => ia, ia => Gaussian.FromMeanAndVariance(0.0, 1.0)),
                WeightPrecisions =
                    buckets.ToDictionary(ia => ia, ia => Gamma.PointMass(point)),
                Thresholds =
                    userNames.ToDictionary(
                        ia => ia,
                        ia => Gaussian.FromMeanAndVariance(0.0, thresholdAndNoiseVariance)),
                NoiseVariance = thresholdAndNoiseVariance
            };
        }

        /// <summary>
        /// Priors from posteriors with possible missing features (e.g. for community training).
        /// </summary>
        /// <param name="posteriors">The posteriors.</param>
        /// <param name="featureSet">The feature set.</param>
        /// <param name="precisionShape">The precision shape.</param>
        /// <param name="precisionScale">The precision scale.</param>
        /// <param name="thresholdAndNoiseVariance">The noise variance.</param>
        /// <param name="userNames">The user names.</param>
        /// <returns>The <see cref="CommunityPriors" />.</returns>
        internal static CommunityPriors FromPosteriors(
            CommunityPosteriors posteriors,
            FeatureSet featureSet,
            double precisionShape,
            double precisionScale,
            double thresholdAndNoiseVariance,
            IList<string> userNames)
        {
            return new CommunityPriors
                       {
                           WeightMeans =
                               featureSet.FeatureBuckets.ToDictionary(
                                   ia => ia,
                                   ia =>
                                   posteriors.WeightMeans.ContainsKey(ia)
                                       ? posteriors.WeightMeans[ia]
                                       : Gaussian.FromMeanAndVariance(0.0, 1.0)),
                           WeightPrecisions =
                               featureSet.FeatureBuckets.ToDictionary(
                                   ia => ia,
                                   ia =>
                                   posteriors.WeightPrecisions.ContainsKey(ia)
                                       ? posteriors.WeightPrecisions[ia]
                                       : Gamma.FromShapeAndScale(precisionShape, precisionScale)),
                           Thresholds =
                               userNames.ToDictionary(
                                   ia => ia,
                                   ia =>
                                   posteriors.Thresholds != null && posteriors.Thresholds.ContainsKey(ia)
                                       ? posteriors.Thresholds[ia]
                                       : Gaussian.FromMeanAndVariance(0.0, thresholdAndNoiseVariance)),
                           NoiseVariance = thresholdAndNoiseVariance,
                       };
        }

        /// <summary>
        /// Priors from posteriors with possible missing features (e.g. for community training).
        /// </summary>
        /// <param name="posteriors">The posteriors.</param>
        /// <param name="featureSet">The feature set.</param>
        /// <param name="point">The point.</param>
        /// <param name="thresholdAndNoiseVariance">The threshold and noise variance.</param>
        /// <param name="userNames">The user names.</param>
        /// <returns>The <see cref="CommunityPriors" />.</returns>
        internal static CommunityPriors FromPosteriors(
            CommunityPosteriors posteriors,
            FeatureSet featureSet,
            double point,
            double thresholdAndNoiseVariance,
            IList<string> userNames)
        {
            return new CommunityPriors
            {
                WeightMeans =
                    featureSet.FeatureBuckets.ToDictionary(
                        ia => ia,
                        ia =>
                        posteriors.WeightMeans.ContainsKey(ia)
                            ? posteriors.WeightMeans[ia]
                            : Gaussian.FromMeanAndVariance(0.0, 1.0)),
                WeightPrecisions =
                    featureSet.FeatureBuckets.ToDictionary(
                        ia => ia,
                        ia =>
                        posteriors.WeightPrecisions.ContainsKey(ia)
                            ? posteriors.WeightPrecisions[ia]
                            : Gamma.PointMass(point)),
                Thresholds =
                    userNames.ToDictionary(
                        ia => ia,
                        ia =>
                        posteriors.Thresholds != null && posteriors.Thresholds.ContainsKey(ia)
                            ? posteriors.Thresholds[ia]
                            : Gaussian.FromMeanAndVariance(0.0, thresholdAndNoiseVariance)),
                NoiseVariance = thresholdAndNoiseVariance,
            };
        }
    }

    /// <summary>
    /// The posteriors.
    /// </summary>
    public class CommunityPosteriors : CommunityVariables
    {
        /// <summary>
        /// Gets the shared means means.
        /// </summary>
        public Dictionary<string, double> SharedMeansMeans
        {
            get
            {
                return this.WeightMeans == null
                           ? null
                           : this.WeightMeans.Where(ia => ia.Key.Feature.IsShared)
                                 .ToDictionary(ia => ia.Key.ToString(), ia => ia.Value.GetMean());
            }
        }

        /// <summary>
        /// Gets the shared mean and standard deviation means.
        /// </summary>
        public IList<PointWithBounds<string>> SharedMeanAndStandardDeviationMeans
        {
            get
            {
                if (this.WeightMeans == null || this.WeightPrecisions == null)
                {
                    return null;
                }

                var meansAndPrecisions =
                    this.WeightMeans.Zip(this.WeightPrecisions, (mean, precision) => new { mean, precision })
                        .Where(ia => ia.mean.Key.Feature.IsShared);

                return meansAndPrecisions.Select(
                    ia =>
                    new PointWithBounds<string>
                        {
                            X = ia.mean.Key.ToString(),
                            Y = ia.mean.Value.GetMean(),
                            Upper = ia.mean.Value.GetMean() + Math.Sqrt(1.0 / ia.precision.Value.GetMean()),
                            Lower = ia.mean.Value.GetMean() - Math.Sqrt(1.0 / ia.precision.Value.GetMean())
                        }).ToArray();
            }
        }

        /// <summary>
        /// Gets the shared mean and precision means.
        /// </summary>
        public IList<PointWithBounds<string>> SharedMeanAndPrecisionMeans
        {
            get
            {
                if (this.WeightMeans == null || this.WeightPrecisions == null)
                {
                    return null;
                }

                var meansAndPrecisions =
                    this.WeightMeans.Zip(this.WeightPrecisions, (mean, precision) => new { mean, precision })
                        .Where(ia => ia.mean.Key.Feature.IsShared);

                return meansAndPrecisions.Select(
                    ia =>
                    new PointWithBounds<string>
                    {
                        X = ia.mean.Key.ToString(),
                        Y = ia.mean.Value.GetMean(),
                        Upper = ia.mean.Value.GetMean() + ia.precision.Value.GetMean(),
                        Lower = ia.mean.Value.GetMean() - ia.precision.Value.GetMean()
                    }).ToArray();
            }
        }

        /// <summary>
        /// Gets the shared means means.
        /// </summary>
        public Dictionary<string, double> SharedPrecisionMeans
        {
            get
            {
                return this.WeightPrecisions == null
                           ? null
                           : this.WeightPrecisions.Where(ia => ia.Key.Feature.IsShared)
                                 .ToDictionary(ia => ia.Key.ToString(), ia => ia.Value.GetMean());
            }
        }

        /// <summary>
        /// Gets the shared variance means.
        /// </summary>
        public Dictionary<string, double> SharedVarianceMeans
        {
            get
            {
                return this.SharedPrecisionMeans == null 
                    ? null 
                    : this.SharedPrecisionMeans.ToDictionary(ia => ia.Key, ia => 1.0 / ia.Value);
            }
        }
    }
}