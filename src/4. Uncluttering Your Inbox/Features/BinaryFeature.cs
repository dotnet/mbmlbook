// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System.Collections.Generic;


    /// <summary>
    /// A binary feature (or multiple related binary features)
    /// of a message used to determine if the user is likely to reply to it.
    /// </summary>
    public abstract class BinaryFeature : Feature
    {
        /// <summary>
        /// Computes the feature.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>
        /// The <see cref="System.Boolean" />
        /// </returns>
        public abstract bool ComputeFeature(Message message);

        /// <summary>
        /// Computes the feature.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="message">The message.</param>
        /// <returns>
        /// The active features
        /// </returns>
        public sealed override IList<FeatureBucketValuePair> Compute(User user, Message message)
        {
            return new[] { new FeatureBucketValuePair { Bucket = this.Buckets[0], Value = this.ComputeFeature(message) ? 1.0 : 0.0 } };
        }

        /// <summary>
        /// Configure the feature.
        /// </summary>
        public override void Configure()
        {
            this.Buckets = new List<FeatureBucket> { new FeatureBucket { Index = 0, Name = this.GetType().Name, Feature = this } };
        }
    }
}