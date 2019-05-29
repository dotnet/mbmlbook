// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System.Collections.Generic;


    /// <summary>
    /// The Feature interface.
    /// </summary>
    public interface IFeature
    {
        /// <summary>
        /// Gets or sets the buckets.
        /// </summary>
        List<FeatureBucket> Buckets { get; set; }

        /// <summary>
        /// Gets the base type name.
        /// </summary>
        string BaseTypeName { get; }

        /// <summary>
        /// Gets or sets a value indicating whether is shared.
        /// </summary>
        bool IsShared { get; set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the count.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Configure the feature.
        /// </summary>
        void Configure();

        /// <summary>
        /// Computes the feature.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="message">The message.</param>
        /// <returns>The active buckets with the values.</returns>
        IList<FeatureBucketValuePair> Compute(User user, Message message);

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <param name="i">The index.</param>
        /// <returns>The <see cref="string"/>.</returns>
        string GetDescription(int i);
    }
}