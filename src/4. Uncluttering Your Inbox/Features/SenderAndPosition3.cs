// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;


    using Microsoft.ML.Probabilistic.Collections;

    /// <summary>
    /// The sender and position composite feature.
    /// </summary>
    public class SenderAndPosition3 : OneOfNFeature, ICompoundFeature<Sender, ToLine>, IConfigurableFeature
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SenderAndPosition3"/> class.
        /// </summary>
        public SenderAndPosition3()
        {
            this.Description = "Who the message is from and your position on the to line";
            
            this.Feature1 = new Sender();
            this.Feature2 = new ToLine();

            // Set this feature to be shared if both base features are shared
            this.IsShared = this.Feature1.IsShared && this.Feature2.IsShared;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SenderAndPosition3" /> class.
        /// </summary>
        /// <param name="user">The user.</param>
        public SenderAndPosition3(User user) : this()
        {
            this.Configure(user);
        }

        /// <summary>
        /// Gets or sets the feature 1.
        /// </summary>
        public Sender Feature1 { get; set; }

        /// <summary>
        /// Gets or sets the feature 2.
        /// </summary>
        public ToLine Feature2 { get; set; }

        /// <summary>
        /// Configures the specified user.
        /// </summary>
        /// <param name="user">The user.</param>
        public void Configure(User user)
        {
            this.Feature1.Configure(user);

            int i = 0;
            foreach (FeatureBucket bucket1 in this.Feature1.Buckets)
            {
                foreach (bool b in new[] { false, true })
                {
                    this.Buckets.Add(
                        new FeatureBucket
                        {
                            Index = i++,
                            Name = string.Format("{0}, {1}={2}", bucket1.Name, this.Feature2.Name, b),
                            Feature = this,
                            Item = new Pair<FeatureBucket, bool>(bucket1, b)
                        });
                }
            }
        }

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
            var b = this.Feature2.ComputeFeature(message);

            var bucket = this.Buckets.FirstOrDefault(ia => ia.Item.First == bucket1 && ia.Item.Second == b);

            if (bucket == null)
            {
                // A new bucket must have been added to one of the base features
                // Add a new bucket and return it
                bucket = new FeatureBucket
                             {
                                 Index = this.Count,
                                 Name = string.Format("{0}, {1}={2}", bucket1.Name, this.Feature2.Name, b),
                                 Item = new Pair<FeatureBucket, bool>(bucket1, b),
                                 Feature = this
                             };

                this.Buckets.Add(bucket);
            }

            return bucket;
        }
    }
}
