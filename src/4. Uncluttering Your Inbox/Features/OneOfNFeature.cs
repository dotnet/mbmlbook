// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;


    /// <summary>
    /// A 1-of-N feature of a message used to determine if the user is likely to reply to it.
    /// </summary>
    [Serializable, System.Runtime.InteropServices.GuidAttribute("DB09AC1A-64CC-4097-B34B-9C1E646F3BCF")]
    public abstract class OneOfNFeature : Feature
    {
        /// <summary>
        /// Gets or sets the bucket names.
        /// </summary>
        public string[][] BucketNames { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether include other.
        /// </summary>
        public bool IncludeOther { get; set; }

        /// <summary>
        /// Gets or sets the feature bucket function.
        /// </summary>
        public Func<string[], int, FeatureBucket> FeatureBucketFunc { get; set; }

        /// <summary>
        /// Computes the feature.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="message">The message.</param>
        /// <returns>The <see cref="FeatureBucket"/></returns>
        public abstract FeatureBucket ComputeFeature(User user, Message message);

        /// <summary>
        /// Computes the feature.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="message">The message.</param>
        /// <returns>
        /// The active buckets with the values
        /// </returns>
        public sealed override IList<FeatureBucketValuePair> Compute(User user, Message message)
        {
            return new[] { new FeatureBucketValuePair { Bucket = this.ComputeFeature(user, message), Value = 1.0 } };
        }

        /// <summary>
        /// Configure the feature.
        /// </summary>
        public override void Configure()
        {
            if (this.BucketNames != null && this.FeatureBucketFunc != null)
            {
                this.Buckets = this.BucketNames.Select(this.FeatureBucketFunc).ToList();
            }

            if (this.IncludeOther)
            {
                this.Buckets.Add(new FeatureBucket { Index = this.Buckets.Count, Name = "Other", Feature = this });
            }
        }
    }
}