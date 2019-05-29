// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;
    using System.Collections.Generic;


    /// <summary>
    /// An M-of-N feature of a message used to determine if the user is likely to reply to it.
    /// </summary>
    [Serializable, System.Runtime.InteropServices.GuidAttribute("DB09AC1A-64CC-4097-B34B-9C1E646F3BCF")]
    public abstract class MofNFeature : Feature, IConfigurableFeature
    {
        /// <summary>
        /// Computes the feature.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="message">The message.</param>
        /// <returns>The <see cref="FeatureBucket"/></returns>
        public abstract IList<FeatureBucketValuePair> ComputeFeature(User user, Message message);

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
            return this.ComputeFeature(user, message);
        }

        /// <summary>
        /// Configure the feature.
        /// </summary>
        public override void Configure()
        {
        }

        /// <summary>
        /// Configures the specified user.
        /// </summary>
        /// <param name="user">The user.</param>
        public abstract void Configure(User user);
    }
}