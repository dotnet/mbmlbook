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
    /// The sender feature with the To line and Cc line separated out.
    /// </summary>
    [Serializable]
    public class SenderToCc : OneOfNFeature, IConfigurableFeature
    {
        /// <summary>
        /// The unknown person.
        /// </summary>
        private readonly Person unknown = User.UnknownPerson;

        /// <summary>
        /// Initializes a new instance of the <see cref="SenderToCc" /> class.
        /// </summary>
        public SenderToCc()
        {
            this.Description = "Who the message is from (To or Cc)";
            this.StringFormat = "{0}";
            this.IsShared = false;
            
            this.BucketDict = new Dictionary<User, Dictionary<string, FeatureBucket>>();
            this.BucketDictCc = new Dictionary<User, Dictionary<string, FeatureBucket>>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SenderToCc" /> class.
        /// </summary>
        /// <param name="user">The user.</param>
        public SenderToCc(User user) : this()
        {
            this.Configure(user);
        }

        /// <summary>
        /// Gets or sets the bucket dictionary.
        /// </summary>
        [XmlIgnore]
        public Dictionary<User, Dictionary<string, FeatureBucket>> BucketDict { get; set; }

        /// <summary>
        /// Gets or sets the bucket dictionary for Cc.
        /// </summary>
        [XmlIgnore]
        public Dictionary<User, Dictionary<string, FeatureBucket>> BucketDictCc { get; set; }

        /// <summary>
        /// Configures the specified users.
        /// </summary>
        /// <param name="user">The user.</param>
        public void Configure(User user)
        {
            var contacts = user.TrainContacts;
            contacts.Insert(0, this.unknown);
            contacts.Insert(1, user);

            var userBucketDict = new Dictionary<string, FeatureBucket>();
            var userBucketDictCc = new Dictionary<string, FeatureBucket>();
            this.BucketDict[user] = userBucketDict;
            this.BucketDictCc[user] = userBucketDictCc;

            foreach (var person in contacts)
            {
                this.AddBucket(user, person, false);
                this.AddBucket(user, person, true);
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
        /// <exception cref="UnclutteringYourInbox.Features.FeatureSet.FeatureException">Feature was not configured</exception>
        public override FeatureBucket ComputeFeature(User user, Message message)
        {
            if (message.SentTo.Any(ia => ia.IsMe))
            {
                var userBucketDict = this.BucketDict[user];
                if (userBucketDict.ContainsKey(message.Sender.Person.BestName.ToString()))
                {
                    return userBucketDict[message.Sender.Person.BestName.ToString()];
                }

                foreach (var identity in message.Sender.Person.Identities)
                {
                    if (userBucketDict.ContainsKey(identity.Email.ToString()))
                    {
                        return userBucketDict[message.Sender.Email.ToString()];
                    }
                }

                return this.AddBucket(user, message.Sender.Person, true);
            }
            else
            {
                var userBucketDict = this.BucketDictCc[user];
                if (userBucketDict.ContainsKey(message.Sender.Person.BestName.ToString()))
                {
                    return userBucketDict[message.Sender.Person.BestName.ToString()];
                }

                foreach (var identity in message.Sender.Person.Identities)
                {
                    if (userBucketDict.ContainsKey(identity.Email.ToString()))
                    {
                        return userBucketDict[message.Sender.Email.ToString()];
                    }
                }

                // Otherwise add new bucket
                return this.AddBucket(user, message.Sender.Person, false);
            }
        }

        /// <summary>
        /// Adds the bucket.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="person">The person.</param>
        /// <param name="cc">if set to <c>true</c> [cc].</param>
        /// <returns>
        /// The <see cref="FeatureBucket" />
        /// </returns>
        private FeatureBucket AddBucket(User user, Person person, bool cc)
        {
            string personName = person.Name + (cc ? "(Cc)" : string.Empty);
            var bucket = new FeatureBucket { Index = this.Buckets.Count, Name = personName, Feature = this };
            this.Buckets.Add(bucket);

            var bucketDict = cc ? this.BucketDictCc : this.BucketDict;
            bucketDict[user][personName] = bucket;
            foreach (var email in
                person.Identities.Select(identity => identity.Email.ToString()).Where(email => !bucketDict[user].ContainsKey(email)))
            {
                bucketDict[user][email] = bucket;
            }

            return bucket;
        }
    }
}
