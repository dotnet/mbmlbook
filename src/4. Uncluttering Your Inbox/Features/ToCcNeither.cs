// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;


    /// <summary>
    /// The on to line or Cc line feature.
    /// </summary>
    [Serializable]
    public class ToCcNeither : OneOfNFeature
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToCcNeither" /> class.
        /// </summary>
        public ToCcNeither()
        {
            this.Description = "Your are on the To or Cc lines";
            this.StringFormat = "{0}";
            this.BucketNames = new[] { new[] { "Neither" }, new[] { "To" }, new[] { "Cc" } };
            this.FeatureBucketFunc = (ia, i) => new FeatureBucket { Index = i, Name = ia[0], Feature = this };
        }

        /// <summary>
        /// Computes the specified vector.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="message">The message.</param>
        /// <returns>
        /// The index of the feature that is on.
        /// </returns>
        /// <exception cref="FeatureSet.FeatureException">Feature was not configured</exception>
        public override FeatureBucket ComputeFeature(User user, Message message)
        {
            if (this.Count == 0)
            {
                throw new FeatureSet.FeatureException("Feature was not configured");
            }

            return this.Buckets[GetPosition(message)];
        }

        /// <summary>
        /// Gets the position on the to line.
        /// </summary>
        /// <param name="m">The message.</param>
        /// <returns>
        /// The Position
        /// </returns>
        internal static int GetPosition(Message m)
        {
            if (m.SentTo.Any(cd => cd.IsMe))
            {
                return 1;
            }

            if (m.CopiedTo.Any(cd => cd.IsMe))
            {
                return 2;
            }

            return 0;
        }
    }
}
