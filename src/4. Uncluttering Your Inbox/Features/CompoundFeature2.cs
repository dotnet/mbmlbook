// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{

    /// <summary>
    /// The compound feature.
    /// </summary>
    public class CompoundFeature2 : OneOfNFeature, ICompoundFeature<ToLine, FromManager>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompoundFeature2"/> class.
        /// </summary>
        public CompoundFeature2()
        {
            this.BucketNames = new[] { new[] { "False, False" }, new[] { "False, True" }, new[] { "True, False" }, new[] { "True, True" } };
            this.FeatureBucketFunc = (ia, i) => new FeatureBucket { Index = i, Feature = this, Name = ia[0] };
        }

        /// <summary>
        /// Gets or sets the feature 1.
        /// </summary>
        public ToLine Feature1 { get; set; }

        /// <summary>
        /// Gets or sets the feature 2.
        /// </summary>
        public FromManager Feature2 { get; set; }

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
            var b1 = this.Feature1.ComputeFeature(message);
            var b2 = this.Feature2.ComputeFeature(message);

            return b1 ? (b2 ? this.Buckets[3] : this.Buckets[2]) : (b2 ? this.Buckets[1] : this.Buckets[0]);
        }
    }
}