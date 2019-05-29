// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;


    /// <summary>
    /// The recipient feature.
    /// </summary>
    [Serializable]
    public sealed class Recipient : MofNFeature
    {
        /// <summary>
        /// The unknown person.
        /// </summary>
        private readonly Person unknown = User.UnknownPerson;

        /// <summary>
        /// Initializes a new instance of the <see cref="Recipient" /> class.
        /// </summary>
        public Recipient()
        {
            this.Description = "Who the message is to";
            this.StringFormat = "{0}";
            this.IsShared = false;

            this.Buckets = new List<FeatureBucket>();
            this.BucketDict = new Dictionary<User, Dictionary<Uncertain<string>, FeatureBucket>>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Recipient" /> class.
        /// </summary>
        /// <param name="user">The user.</param>
        public Recipient(User user)
            : this()
        {
            this.Configure(user);
        }

        /// <summary>
        /// Gets or sets the bucket dictionary.
        /// </summary>
        [XmlIgnore]
        public Dictionary<User, Dictionary<Uncertain<string>, FeatureBucket>> BucketDict { get; set; }

        /// <summary>
        /// Configures the specified user.
        /// </summary>
        /// <param name="user">The user.</param>
        public override void Configure(User user)
        {
            var contacts = user.TrainContacts;
            contacts.Insert(0, this.unknown);
            contacts.Insert(1, user);

            this.BucketDict[user] = new Dictionary<Uncertain<string>, FeatureBucket>();

            foreach (var person in contacts)
            {
                Sender.CreateBucket(this, user, person, this.BucketDict[user]);
            }
        }
        
        /// <summary>
        /// Computes the feature.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="message">The message.</param>
        /// <returns>
        /// The active bucket.
        /// </returns>
        /// <exception cref="FeatureSet.FeatureException">Feature was not configured</exception>
        public override IList<FeatureBucketValuePair> ComputeFeature(User user, Message message)
        {
            if (!this.BucketDict.ContainsKey(user))
            {
                this.BucketDict[user] = new Dictionary<Uncertain<string>, FeatureBucket>();
            }

            var buckets = message.Recipients.Select(recipient => Sender.GetBucketForPerson(user, recipient, this.BucketDict[user])
                 ?? Sender.CreateBucket(this, user, recipient, this.BucketDict[user])).ToList();

            return buckets.Select(ia => new FeatureBucketValuePair { Bucket = ia, Value = 1.0 / buckets.Count }).ToList();
        }
    }
}
