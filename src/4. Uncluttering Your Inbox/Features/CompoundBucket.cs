// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    /// <summary>
    /// The compound bucket.
    /// </summary>
    public class CompoundBucket
    {
        /// <summary>
        /// Gets or sets the bucket 1.
        /// </summary>
        public FeatureBucket Bucket1 { get; set; }

        /// <summary>
        /// Gets or sets the bucket 2.
        /// </summary>
        public FeatureBucket Bucket2 { get; set; }
    }
}