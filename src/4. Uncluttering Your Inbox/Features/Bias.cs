// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;
    using System.Collections.Generic;


    /// <summary>
    /// The bias.
    /// </summary>
    public class Bias : Feature
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Bias"/> class.
        /// </summary>
        public Bias()
            : this(-Math.Sqrt(10))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bias"/> class.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        public Bias(double value)
        {
            this.Value = value;
            this.IsShared = true;
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Configure the feature.
        /// </summary>
        public override void Configure()
        {
            this.Buckets = new List<FeatureBucket> { new FeatureBucket { Index = 0, Name = "Bias", Item = this.Value, Feature = this } };
        }

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
            return new[] { new FeatureBucketValuePair { Bucket = this.Buckets[0], Value = this.Buckets[0].Item } };
        }
    }
}
