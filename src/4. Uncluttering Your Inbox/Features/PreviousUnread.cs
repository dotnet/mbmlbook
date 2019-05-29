// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;


    /// <summary>
    /// The previous unread.
    /// </summary>
    public class PreviousUnread : OneOfNFeature
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PreviousUnread"/> class.
        /// </summary>
        public PreviousUnread() : this(FeatureSet.PreviousUnreadStrings)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviousUnread"/> class.
        /// </summary>
        /// <param name="previousUnreadStrings">
        /// The previous unread strings.
        /// </param>
        public PreviousUnread(IEnumerable<string> previousUnreadStrings)
        {
            this.Description = "The number of unread messages prior to this one";
            this.StringFormat = "{0}";
            this.IsShared = true;

            this.BucketNames = previousUnreadStrings.Select(ia => new[] { ia }).ToArray();
            this.FeatureBucketFunc = (ia, i) => new FeatureBucket { Index = i, Name = ia[0], Feature = this };
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
            var previous = message.Conversation.Messages.TakeWhile(ia => ia != message).ToList();

            if (previous.Count == 0)
            {
                return this.Buckets[0];
            }

            var unread = previous.Count(ia => !ia.Sender.IsMe && ia.IsRead);

            return unread < this.Buckets.Count - 2 ? this.Buckets[unread + 1] : this.Buckets.Last();
        }
    }
}
