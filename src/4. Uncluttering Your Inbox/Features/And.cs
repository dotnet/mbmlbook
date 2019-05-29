// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;
    using System.Collections.Generic;


    /// <summary>
    /// The binary and feature - feature that is the "and" of two other binary features
    /// </summary>
    [Serializable]
    public sealed class And : BinaryFeature, ICompoundFeature<BinaryFeature, BinaryFeature>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="And"/> class. 
        /// </summary>
        public And()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="And"/> class.
        /// </summary>
        /// <param name="feature1">The first feature.</param>
        /// <param name="feature2">The second feature.</param>
        public And(BinaryFeature feature1, BinaryFeature feature2)
        {
            this.Feature1 = feature1;
            this.Feature2 = feature2;
            this.Description = feature1.Description  + " AND " + feature2.Description;
            this.Configure();
        }

        /// <summary>
        /// Gets or sets the base feature 1.
        /// </summary>
        public BinaryFeature Feature1 { get; set; }

        /// <summary>
        /// Gets or sets the base feature 2.
        /// </summary>
        public BinaryFeature Feature2 { get; set; }

        /// <summary>
        /// Configure the feature.
        /// </summary>
        public override void Configure()
        {
            this.Buckets = new List<FeatureBucket>
                               {
                                   new FeatureBucket
                                       {
                                           Name =
                                               string.Format(
                                                   "{0} AND {1}",
                                                   this.Feature1.Description,
                                                   this.Feature2.Description),
                                           Index = 0,
                                           Feature = this
                                       }
                               };
        }

        /// <summary>
        /// Computes the feature.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>
        /// The <see cref="System.Boolean" />
        /// </returns>
        public override bool ComputeFeature(Message message)
        {
            return this.Feature1.ComputeFeature(message) && this.Feature2.ComputeFeature(message);
        }
    }
}
