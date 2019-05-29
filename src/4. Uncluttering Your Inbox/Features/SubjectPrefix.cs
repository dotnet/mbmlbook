// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;
    using System.Linq;


    /// <summary>
    /// The subject starts with feature.
    /// </summary>
    [Serializable]
    public class SubjectPrefix : OneOfNFeature
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubjectPrefix"/> class. 
        /// </summary>
        public SubjectPrefix()
            : this(FeatureSet.SubjectPrefixes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubjectPrefix"/> class.
        /// </summary>
        /// <param name="prefixes">The prefixes.</param>
        public SubjectPrefix(string[][] prefixes)
        {
            this.Description = "Subject starts with <{0}>";
            this.BucketNames = prefixes;
            this.IncludeOther = true;
            this.FeatureBucketFunc = (ia, i) => new FeatureBucket { Index = i, Name = ia[0], Feature = this, Item = ia };
        }

        /// <summary>
        /// Computes the feature.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="message">The message.</param>
        /// <returns>
        /// The active bucket.
        /// </returns>
        public override FeatureBucket ComputeFeature(User user, Message message)
        {
            if (string.IsNullOrEmpty(message.SubjectPrefix))
            {
                return this.Buckets[0];
            }

            // FeatureNames[0] is the catch-all bin
            // Note this finds the first matching prefix only.
            foreach (var bucket in Buckets.Skip(1).Take(this.Buckets.Count - 2))
            {
                foreach (string prefix in bucket.Item)
                {
                    if (message.SubjectPrefix == prefix)
                    {
                        return bucket;
                    }
                }
            }
            
            return this.Buckets.Last();
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(this.Description, this.FeatureNames == null ? "..." : string.Join(", ", this.FeatureNames.Skip(1)));
        }
    }
}
