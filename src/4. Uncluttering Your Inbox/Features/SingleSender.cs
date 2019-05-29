// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;


    /// <summary>
    /// The single sender feature.
    /// </summary>
    [Serializable]
    public class SingleSender : BinaryFeature, IConfigurableFeature
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SingleSender" /> class.
        /// </summary>
        public SingleSender()
        {
            this.Description = "The message is from";
            this.StringFormat = "{0}";
            this.UserCount = 1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleSender"/> class.
        /// </summary>
        /// <param name="user">The user.</param>
        public SingleSender(User user) : this()
        {
            this.Configure(user);
        }

        /// <summary>
        /// Gets or sets the user count.
        /// </summary>
        public int UserCount { get; set; }

        /// <summary>
        /// Configures the specified users.
        /// </summary>
        /// <param name="user">The user.</param>
        public void Configure(User user)
        {
            // Get name of Sender who sends a lot of emails but also Cc's a lot
            var sender =
                user.TrainMessages.GroupBy(ia => ia.Sender.Person)
                    .OrderByDescending(
                        ia => ia.Count(m => m.SentTo.Contains(user.BestIdentity)) * ia.Count(m => m.CopiedTo.Contains(user.BestIdentity)))
                    .First(ia => !ia.Key.IsMe && ia.Count(m => m.IsRepliedTo) > 0)
                    .Key.Name;

            this.Buckets = new List<FeatureBucket>
                               {
                                   new FeatureBucket
                                       {
                                           Index = 0,
                                           Name = sender.ToString(),
                                           Item = sender,
                                           Feature = this
                                       }
                               };
        }

        /// <summary>
        /// Computes the feature.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>
        /// The active bucket.
        /// </returns>
        /// <exception cref="FeatureSet.FeatureException">Feature was not configured</exception>
        public override bool ComputeFeature(Message message)
        {
            return message.Sender.Name.Equals(this.Buckets[0].Item);
        }
        
        /// <summary>
        /// The user person pair.
        /// </summary>
        public class UserPersonPair
        {
            /// <summary>
            /// Gets or sets the user.
            /// </summary>
            public Uncertain<string> UserName { get; set; }

            /// <summary>
            /// Gets or sets the person.
            /// </summary>
            public Uncertain<string> PersonName { get; set; }

            /// <summary>
            /// Returns a <see cref="System.String" /> that represents this instance.
            /// </summary>
            /// <returns>
            /// A <see cref="System.String" /> that represents this instance.
            /// </returns>
            public override string ToString()
            {
                return string.Format("{0}-{1}", this.UserName, this.PersonName);
            }
        }
    }
}
