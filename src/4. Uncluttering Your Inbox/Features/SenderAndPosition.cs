// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;
    using System.Collections.Generic;


    /// <summary>
    /// The sender and position composite feature.
    /// </summary>
    public class SenderAndPosition : CompoundFeature<Sender, ToCcNeither>, IConfigurableFeature
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SenderAndPosition"/> class.
        /// </summary>
        public SenderAndPosition()
        {
            this.Feature1 = new Sender();
            this.Feature2 = new ToCcNeither();

            // Set this feature to be shared if both base features are shared
            this.IsShared = this.Feature1.IsShared && this.Feature2.IsShared;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SenderAndPosition" /> class.
        /// </summary>
        /// <param name="user">The user.</param>
        public SenderAndPosition(User user) : this()
        {
            this.Configure(user);
        }

        /// <summary>
        /// Configures the specified users.
        /// </summary>
        /// <param name="user">The user.</param>
        public void Configure(User user)
        {
            this.Feature1.Configure(user);

            int i = 0;
            foreach (FeatureBucket bucket1 in this.Feature1.Buckets)
            {
                foreach (FeatureBucket bucket2 in this.Feature2.Buckets)
                {
                    this.Buckets.Add(
                        new FeatureBucket
                        {
                            Index = i++,
                            Name = string.Format("{0}, {1}", bucket1.Name, bucket2.Name),
                            Feature = this,
                            Item = new CompoundBucket { Bucket1 = bucket1, Bucket2 = bucket2 }
                        });
                }
            }
        }
    }
}
