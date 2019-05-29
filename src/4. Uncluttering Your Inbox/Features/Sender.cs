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
    /// The sender feature.
    /// </summary>
    [Serializable]
    public class Sender : OneOfNFeature, IConfigurableFeature
    {
        /// <summary>
        /// The unknown person.
        /// </summary>
        private readonly Person unknown = User.UnknownPerson;

        /// <summary>
        /// Initializes a new instance of the <see cref="Sender" /> class.
        /// </summary>
        public Sender()
        {
            this.Description = "Who the message is from";
            this.StringFormat = "{0}";
            this.IsShared = false;
            
            this.BucketDict = new Dictionary<User, Dictionary<Uncertain<string>, FeatureBucket>>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sender" /> class.
        /// </summary>
        /// <param name="user">The user.</param>
        public Sender(User user) : this()
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
        public void Configure(User user)
        {
            var contacts = user.TrainContacts;
            contacts.Insert(0, this.unknown);
            contacts.Insert(1, user);

            this.BucketDict[user] = new Dictionary<Uncertain<string>, FeatureBucket>();

            foreach (var person in contacts)
            {
                CreateBucket(this, user, person, this.BucketDict[user]);
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
        public override FeatureBucket ComputeFeature(User user, Message message)
        {
            if (!this.BucketDict.ContainsKey(user))
            {
                this.BucketDict[user] = new Dictionary<Uncertain<string>, FeatureBucket>();
            }

            return GetBucketForPerson(user, message.Sender.Person, this.BucketDict[user])
                   ?? CreateBucket(this, user, message.Sender.Person, this.BucketDict[user]);
        }

        /// <summary>
        /// Gets the bucket for person.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="person">The person.</param>
        /// <param name="userBucketDict">The user bucket dictionary.</param>
        /// <returns>
        /// The <see cref="FeatureBucket" />
        /// </returns>
        internal static FeatureBucket GetBucketForPerson(User user, Person person, Dictionary<Uncertain<string>, FeatureBucket> userBucketDict)
        {
            FeatureBucket bucket;
            userBucketDict.TryGetValue(person.BestName, out bucket);

            if (bucket != null)
            {
                return bucket;
            }

            foreach (var email in person.Identities.Select(identity => identity.Email))
            {
                userBucketDict.TryGetValue(email, out bucket);

                if (bucket != null)
                {
                    return bucket;
                }
            }

            return null;
        }

        /// <summary>
        /// Adds the bucket.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <param name="user">The user.</param>
        /// <param name="person">The person.</param>
        /// <param name="userBucketDict">The user bucket dictionary.</param>
        /// <returns>
        /// The <see cref="FeatureBucket" />
        /// </returns>
        internal static FeatureBucket CreateBucket(Feature feature, User user, Person person, Dictionary<Uncertain<string>, FeatureBucket> userBucketDict)
        {
            var personName = person.Name;
            var bucket = new FeatureBucket { Index = feature.Buckets.Count, Name = personName.ToString(), Feature = feature };
            feature.Buckets.Add(bucket);

            userBucketDict[personName] = bucket;
            foreach (var email in
                person.Identities.Select(identity => identity.Email).Where(email => !userBucketDict.ContainsKey(email)))
            {
                userBucketDict[email] = bucket;
            }

            return bucket;
        }
    }
}
