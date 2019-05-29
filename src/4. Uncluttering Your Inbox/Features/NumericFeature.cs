// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;


    /// <summary>
    ///     A 1-of-N feature of a message used to determine if the user is likely to reply to it.
    /// </summary>
    public abstract class NumericFeature : Feature
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumericFeature"/> class.
        /// </summary>
        protected NumericFeature()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NumericFeature"/> class.
        /// </summary>
        /// <param name="bins">The bins.</param>
        protected NumericFeature(int[] bins)
        {
            this.Bins = bins;
        }

        /// <summary>
        /// Gets or sets the bins.
        /// </summary>
        public int[] Bins { get; set; }

        /// <summary>
        /// Computes the feature.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="message">The message.</param>
        /// <returns>
        /// The <see cref="System.Boolean" />
        /// </returns>
        public sealed override IList<FeatureBucketValuePair> Compute(User user, Message message)
        {
            return new[] { new FeatureBucketValuePair { Bucket = this.Buckets[this.ComputeFeature(message)], Value = 1.0 } };
        }

        /// <summary>
        /// Computes the feature.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>
        /// The index of the feature that is on.
        /// </returns>
        public abstract int ComputeFeature(Message message);

        /// <summary>
        /// Configure the feature.
        /// </summary>
        public override void Configure()
        {
            this.Buckets =
                this.Bins.Select(
                    (ia, i) => new FeatureBucket { Index = i, Name = GetLengthStrings(this.Bins, i), Feature = this, Item = ia }).ToList();
        }

        /// <summary>
        /// Gets the length strings.
        /// </summary>
        /// <param name="lengths">The lengths.</param>
        /// <param name="i">The i.</param>
        /// <returns>
        /// The string.
        /// </returns>
        internal static string GetLengthStrings(int[] lengths, int i)
        {
            if (i == 0)
            {
                return lengths[0].ToString(CultureInfo.InvariantCulture);
            }

            if (lengths[i] - lengths[i - 1] == 1)
            {
                return lengths[i].ToString(CultureInfo.InvariantCulture);
            }

            return lengths[i] < int.MaxValue
                       ? string.Format("{0}-{1}", lengths[i - 1] + 1, lengths[i])
                       : string.Format(">{0}", lengths[i - 1]);
        }
    }
}