// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;
    using System.Linq;


    /// <summary>
    /// The compound feature.
    /// </summary>
    /// <typeparam name="TFeature1">The type of feature 1</typeparam>
    /// <typeparam name="TFeature2">The type of feature 2.</typeparam>
    public class CompoundFeature<TFeature1, TFeature2> : OneOfNFeature, ICompoundFeature<TFeature1, TFeature2>
        where TFeature1 : OneOfNFeature where TFeature2 : OneOfNFeature
    {
        /// <summary>
        /// Gets or sets the feature 1.
        /// </summary>
        public TFeature1 Feature1 { get; set; }

        /// <summary>
        /// Gets or sets the feature 2.
        /// </summary>
        public TFeature2 Feature2 { get; set; }

        /// <summary>
        /// Computes the feature.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="message">The message.</param>
        /// <returns>
        /// The <see cref="FeatureBucket" />
        /// </returns>
        public override FeatureBucket ComputeFeature(User user, Message message)
        {
            var bucket1 = this.Feature1.ComputeFeature(user, message);
            var bucket2 = this.Feature2.ComputeFeature(user, message);

            var bucket = this.Buckets.FirstOrDefault(ia => ia.Item.Bucket1 == bucket1 && ia.Item.Bucket2 == bucket2);

            if (bucket != null)
            {
                return bucket;
            }

            // A new bucket must have been added to one of the base features
            // Add a new bucket and return it
            bucket = new FeatureBucket
                         {
                             Index = this.Count,
                             Name = string.Format("{0}, {1}", bucket1.Name, bucket2.Name),
                             Item = new CompoundBucket { Bucket1 = bucket1, Bucket2 = bucket2 },
                             Feature = this
                         };

            this.Buckets.Add(bucket);

            return bucket;
        }
    }
}