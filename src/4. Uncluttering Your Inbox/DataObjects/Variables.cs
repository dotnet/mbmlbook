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
    public abstract class Variables
    {
        /// <summary>
        /// The top buckets.
        /// </summary>
        private HashSet<FeatureBucket> topBuckets;

        /// <summary>
        /// Gets or sets the weight priors/posteriors.
        /// </summary>
        public Dictionary<FeatureBucket, Gaussian> Weights { get; set; }

        /// <summary>
        /// Gets the top weights.
        /// </summary>
        public Dictionary<FeatureBucket, Gaussian> TopWeights
        {
            get
            {
                if (this.Weights == null)
                {
                    return null;
                }
                
                if (this.topBuckets == null)
                {
                    this.topBuckets =
                        new HashSet<FeatureBucket>(
                            this.Weights.GroupBy(ia => ia.Key.Feature)
                                .Select(g => g.OrderByDescending(ia => Math.Abs(ia.Value.GetMean())).Take(15))
                                .SelectMany(ia => ia.Select(kvp => kvp.Key)));
                }

                return this.Weights.Where(ia => this.topBuckets.Contains(ia.Key)).ToDictionary(ia => ia.Key, ia => ia.Value);
            }
        }

        /// <summary>
        /// Gets or sets the threshold prior/posterior.
        /// </summary>
        public Gaussian Threshold { get; set; }

        /// <summary>
        /// Gets all the variables as a dictionary (for plotting on the same graph).
        /// </summary>
        public Dictionary<string, Gaussian> Dict
        {
            get
            {
                if (this.Weights == null)
                {
                    return null;
                }

                var dict = this.Weights.ToDictionary(ia => ia.Key.ToString(), ia => ia.Value);
                dict["Threshold"] = this.Threshold;
                return dict;
            }
        }
    }

    /// <summary>
    /// The priors.
    /// </summary>
    public class Priors : Variables
    {
        /// <summary>
        /// Gets or sets the noise prior variance
        /// </summary>
        public double NoiseVariance { get; set; }
        
        /// <summary>
        /// Generate priors from the weight and threshold variance.
        /// </summary>
        /// <param name="featureBuckets">The feature buckets.</param>
        /// <param name="weightVariance">The weight variance.</param>
        /// <param name="thresholdVariance">The threshold variance.</param>
        /// <returns>
        /// The <see cref="Priors" />
        /// </returns>
        internal static Priors Generate(ICollection<FeatureBucket> featureBuckets, double weightVariance, double thresholdVariance)
        {
            return new Priors
                       {
                           Weights = featureBuckets.ToDictionary(ia => ia, ia => Gaussian.FromMeanAndVariance(0.0, weightVariance)),
                           Threshold = Gaussian.FromMeanAndVariance(0.0, thresholdVariance),
                           NoiseVariance = thresholdVariance
                       };
        }

        /// <summary>
        /// Priors from posteriors.
        /// </summary>
        /// <param name="posteriors">The posteriors.</param>
        /// <param name="noiseVariance">The noise variance.</param>
        /// <returns>
        /// The <see cref="Priors" />
        /// </returns>
        internal static Priors FromPosteriors(Posteriors posteriors, double noiseVariance)
        {
            return new Priors
                       {
                           Weights = posteriors.Weights, 
                           Threshold = posteriors.Threshold, 
                           NoiseVariance = noiseVariance
                       };
        }

        /// <summary>
        /// Priors from posteriors with possible missing features.
        /// </summary>
        /// <param name="posteriors">The posteriors.</param>
        /// <param name="featureSet">The feature set.</param>
        /// <param name="noiseVariance">The noise variance.</param>
        /// <returns>The <see cref="Priors" /></returns>
        internal static Priors FromPosteriors(Posteriors posteriors, FeatureSet featureSet, double noiseVariance)
        {
            return new Priors
                       {
                           Weights =
                               featureSet.FeatureBuckets.ToDictionary(
                                   ia => ia,
                                   ia =>
                                   posteriors.Weights.ContainsKey(ia)
                                       ? posteriors.Weights[ia]
                                       : Gaussian.FromMeanAndVariance(0.0, noiseVariance)),
                            Threshold = posteriors.Threshold,
                            NoiseVariance = noiseVariance,
                       };
        }

        /// <summary>
        /// Priors from the community posteriors, using the means of the weight mean and weight precision posteriors.
        /// </summary>
        /// <param name="posteriors">The posteriors.</param>
        /// <param name="featureSet">The feature set.</param>
        /// <param name="thresholdAndNoiseVariance">The threshold and noise variance.</param>
        /// <returns>The <see cref="Priors" /></returns>
        internal static Priors FromCommunityPosteriors(CommunityPosteriors posteriors, FeatureSet featureSet, double thresholdAndNoiseVariance)
        {
            return new Priors
                       {
                           Weights =
                               featureSet.FeatureBuckets.ToDictionary(
                                   ia => ia,
                                   ia =>
                                   posteriors.WeightMeans.ContainsKey(ia) && posteriors.WeightPrecisions.ContainsKey(ia)
                                       ? Gaussian.FromMeanAndPrecision(
                                           posteriors.WeightMeans[ia].GetMean(),
                                           posteriors.WeightPrecisions[ia].GetMean())
                                       : Gaussian.FromMeanAndVariance(0.0, thresholdAndNoiseVariance)),
                           Threshold = Gaussian.FromMeanAndVariance(0.0, thresholdAndNoiseVariance),
                           NoiseVariance = thresholdAndNoiseVariance
                       };
        }
    }

    /// <summary>
    /// The posteriors.
    /// </summary>
    public class Posteriors : Variables
    {
        /// <summary>
        /// Gets the means.
        /// </summary>
        public Dictionary<string, double> Means
        {
            get
            {
                return this.TopWeights == null ? null : this.TopWeights.ToDictionary(ia => ia.Key.ToString(), ia => ia.Value.GetMean());
            }
        }

        /// <summary>
        /// Gets the means and standard deviations.
        /// </summary>
        public IList<PointWithBounds<string>> MeansAndStandardDeviations
        {
            get
            {
                return this.TopWeights == null
                           ? null
                           : this.TopWeights.Select(
                               ia => new PointWithBounds<string>
                                   {
                                       X = ia.Key.ToString(),
                                       Y = ia.Value.GetMean(),
                                       Lower = ia.Value.GetMean() - Math.Sqrt(ia.Value.GetVariance()),
                                       Upper = ia.Value.GetMean() + Math.Sqrt(ia.Value.GetVariance())
                               }).ToArray();
            }
        }

        /// <summary>
        /// Gets the shared means.
        /// </summary>
        public Dictionary<string, double> SharedMeans
        {
            get
            {
                return this.Weights == null
                           ? null
                           : this.Weights.Where(ia => ia.Key.Feature.IsShared)
                                 .ToDictionary(ia => ia.Key.ToString(), ia => ia.Value.GetMean());
            }
        }

        /// <summary>
        /// Gets the shared means and standard deviations.
        /// </summary>
        public IList<PointWithBounds<string>> SharedMeansAndStandardDeviations
        {
            get
            {
                return this.Weights == null
                           ? null
                           : this.Weights.Where(ia => ia.Key.Feature.IsShared)
                                 .Select(
                                     ia =>
                                     new PointWithBounds<string>
                                         {
                                             X = ia.Key.ToString(),
                                             Y = ia.Value.GetMean(),
                                             Lower = ia.Value.GetMean() - Math.Sqrt(ia.Value.GetVariance()),
                                             Upper = ia.Value.GetMean() + Math.Sqrt(ia.Value.GetVariance())
                                         }).ToArray();
            }
        }
    }
}