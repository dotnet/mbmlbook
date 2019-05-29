// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;


    using MBMLViews;

    using Microsoft.ML.Probabilistic.Math;

    /// <summary>
    /// The feature sets.
    /// </summary>
    public enum FeatureSetType
    {
        /// <summary>
        /// The single feature set.
        /// </summary>
        Single,

        /// <summary>
        /// The separate feature set (feature testing).
        /// </summary>
        Separate,

        /// <summary>
        /// The compound feature set (feature testing).
        /// </summary>
        Compound,

        /// <summary>
        /// The initial feature set.
        /// </summary>
        Initial,

        /// <summary>
        /// The initial feature set with subject prefix added.
        /// </summary>
        WithSubjectPrefix,

        /// <summary>
        /// The initial feature set with recipient added.
        /// </summary>
        WithRecipient,

        /// <summary>
        /// The initial feature set with recipient added and a Threshold feature.
        /// </summary>
        WithRecipient2,

        /// <summary>
        /// The combined feature set (all features in initial and final feature set).
        /// </summary>
        Combined
    }

    /// <summary>
    /// A set of features to be used for classifying messages.
    /// By default this will contain the original set of features used by the ReplyToModel.
    /// This list can be edited or replaced to modify the features used.
    /// </summary>
    [Serializable]
    public class FeatureSet : IEquatable<FeatureSet>
    {
        /// <summary>
        /// The subject prefixes.
        /// </summary>
        internal static readonly string[][] SubjectPrefixes =
            {
                new[] { "no prefix" },
                new[] { "re" },
                new[] { "fw", "fwd" }
            };

        /// <summary>
        /// The message lengths.
        /// </summary>
        internal static readonly string[] MessageLengths = { "very, very short", "very short", "short", "quite short", "not short" };

        /// <summary>
        /// The body contents.
        /// </summary>
        internal static readonly string[] BodyContents = { "?", "unsubscribe", "http://", "thank" };

        /// <summary>
        /// The body word count bins.
        /// </summary>
        internal static readonly int[] BodyWordCountBins = { 0, 1, 2, 3, 4, 6, 8, 16, 32, 64, 128, int.MaxValue };

        /// <summary>
        /// The body char length bins.
        /// </summary>
        internal static readonly int[] BodyCharLengthBins = { 0, 4, 8, 16, 32, 64, 128, 256, 512, 1023, int.MaxValue };

        /// <summary>
        /// The subject word count bins.
        /// </summary>
        internal static readonly int[] SubjectWordCountBins = { 0, 1, 2, 4, 8, int.MaxValue };

        /// <summary>
        /// The subject char length bins.
        /// </summary>
        internal static readonly int[] SubjectCharLengthBins = { 0, 2, 4, 8, 16, 32, 64, int.MaxValue };

        /// <summary>
        /// The number of messages.
        /// </summary>
        internal static readonly string[] NumMessages = { "0", "1", "2", "3", "more than 3" };

        /// <summary>
        /// The contributions.
        /// </summary>
        internal static readonly string[] Contributions = { "0", "1", "2", "3", "4", ">4" };

        /// <summary>
        /// The positions.
        /// </summary>
        internal static readonly string[] Positions =
            {
                "received this though a distribution list", 
                "are 1st on the To line", 
                "are 2nd on the To line",
                "are not 1st or 2nd on the To line", 
                "are 1st on the Cc line", 
                "are not 1st on the Cc line"
            };

        /// <summary>
        /// The positions (short version).
        /// </summary>
        internal static readonly string[] PositionsShort =
            {
                "NotOnToOrCcLine", "FirstOnToLine", "SecondOnToLine", "ThirdOrLaterOnToLine",
                "FirstOnCcLine", "SecondOrLaterOnCcLine"
            };

        /// <summary>
        /// The positions (short version).
        /// </summary>
        internal static readonly string[] PositionsShort1 =
            {
                "NotOnToOrCcLine", "FirstAndOnlyOnToLine", "FirstAndOthersOnToLine", "SecondOnToLine", "ThirdOrLaterOnToLine",
                "FirstOnCcLine", "SecondOrLaterOnCcLine"
            };
        
        /// <summary>
        /// The previous unread strings.
        /// </summary>
        internal static readonly string[] PreviousUnreadStrings =
            {
                "NoPrevious", "NoUnread", "OneUnread", "TwoUnread", "ThreeOrMoreUnread"
            };

        /// <summary>
        /// The feature sets.
        /// </summary>
        private static readonly Dictionary<FeatureSetType, Type[]> FeatureSets = new Dictionary<FeatureSetType, Type[]>
            {
                { FeatureSetType.Single,    new[] { typeof(ToLine) } },
                { FeatureSetType.Separate,  new[] { typeof(ToLine), typeof(FromManager) } },
                { FeatureSetType.Initial,   new[]
                                                    {
                                                        typeof(FromMe),
                                                        typeof(ToCcPosition),
                                                        typeof(HasAttachments),
                                                        typeof(BodyLength),
                                                        typeof(SubjectLength),
                                                        typeof(Sender)
                                                    } 
                },
                { FeatureSetType.WithSubjectPrefix, new[]
                                                    {
                                                        typeof(FromMe),
                                                        typeof(ToCcPosition),
                                                        typeof(HasAttachments),
                                                        typeof(BodyLength),
                                                        typeof(SubjectLength),
                                                        typeof(SubjectPrefix),
                                                        typeof(Sender)              
                                                    }
                },
                { FeatureSetType.WithRecipient, new[]
                                                    {
                                                        typeof(FromMe),
                                                        typeof(ToCcPosition),
                                                        typeof(HasAttachments),
                                                        typeof(BodyLength),
                                                        typeof(SubjectLength),
                                                        typeof(SubjectPrefix),
                                                        typeof(Sender),
                                                        typeof(Recipient)
                                                    }
                },
                { FeatureSetType.WithRecipient2, new[]
                                                    {
                                                        typeof(FromMe),
                                                        typeof(ToCcPosition),
                                                        typeof(HasAttachments),
                                                        typeof(BodyLength),
                                                        typeof(SubjectLength),
                                                        typeof(SubjectPrefix),
                                                        typeof(Sender),
                                                        typeof(Recipient),
                                                        typeof(Bias)
                                                    } 
                }
            };

        /// <summary>
        /// The feature buckets.
        /// </summary>
        private OrderedSet<FeatureBucket> featureBuckets;

        /// <summary>
        /// Initializes static members of the <see cref="FeatureSet"/> class. 
        /// </summary>
        static FeatureSet()
        {
            FeatureSets[FeatureSetType.Combined] = FeatureSets.SelectMany(ia => ia.Value).Distinct().ToArray();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureSet"/> class.
        /// </summary>
        public FeatureSet()
        {
            this.Sparsity = Sparsity.Sparse;
            this.Features = new List<IFeature>();
        }

        /// <summary>
        /// Gets or sets the sparsity. Controls the sparsity of the computed feature vectors
        /// </summary>
        public Sparsity Sparsity { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether do not infer (for viewing only).
        /// </summary>
        public bool DoNotInfer { get; set; }

        /// <summary>
        /// Gets the shared features.
        /// </summary>
        public IEnumerable<IFeature> SharedFeatures
        {
            get
            {
                return this.Features == null ? null : this.Features.Where(ia => ia.IsShared);
            }
        }

        /// <summary>
        /// Gets the non shared features.
        /// </summary>
        public IEnumerable<IFeature> NonSharedFeatures
        {
            get
            {
                return this.Features == null ? null : this.Features.Where(ia => !ia.IsShared);
            }
        }

        /// <summary>
        ///     Gets the feature vector length.
        /// </summary>
        public int FeatureVectorLength
        {
            get
            {
                return this.Features == null ? 0 : this.Features.Sum(f => f.Count);
            }
        }

        /// <summary>
        /// Gets the shared feature vector length.
        /// </summary>
        public int SharedFeatureVectorLength
        {
            get
            {
                return this.SharedFeatures == null ? 0 : this.SharedFeatures.Sum(f => f.Count);
            }
        }

        /// <summary>
        /// Gets the non shared feature vector length.
        /// </summary>
        public int NonSharedFeatureVectorLength
        {
            get { return this.NonSharedFeatures == null ? 0 : this.NonSharedFeatures.Sum(f => f.Count); }
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets the features.
        /// </summary>
        public List<IFeature> Features { get; set; }

        /// <summary>
        /// Gets the feature descriptions.
        /// </summary>
        public Dictionary<string, object> FeatureDescriptions
        {
            get
            {
                return this.Features.ToDictionaryForTable<IFeature, string, object, object>(
                    ia => ia.GetType().Name,
                    new Dictionary<string, Func<IFeature, object>>
                        {
                            { "Description", ia => ia.ToString() },
                            { "#Buckets", ia => ia is IConfigurableFeature ? (object)"(varies)" : (object)ia.Count }
                        },
                    false,
                    null,
                    string.Empty);
            }
        }

        /// <summary>
        /// Gets the feature buckets.
        /// </summary>
        public OrderedSet<FeatureBucket> FeatureBuckets
        {
            get
            {
                if (this.Features == null)
                {
                    return null;
                }

                if (this.featureBuckets == null || this.featureBuckets.Count != this.Features.Sum(ia => ia.Buckets.Count))
                {
                    this.featureBuckets = new OrderedSet<FeatureBucket>(this.Features.SelectMany(ia => ia.Buckets));
                }

                return this.featureBuckets;
            }
        }

        /// <summary>
        /// Gets the feature set without a user specified. This is used for the community feature set.
        /// </summary>
        /// <param name="featureSetType">Type of the feature set.</param>
        /// <returns>The <see cref="FeatureSet"/></returns>
        public static FeatureSet GetFeatureSet(FeatureSetType featureSetType)
        {
            var features = new List<IFeature>();
            foreach (var type in FeatureSets[featureSetType])
            {
                var feature = (IFeature)Activator.CreateInstance(type);
                feature.Configure();
            }

            return new FeatureSet
            {
                Name = featureSetType.ToString(),
                Features = features.ToList()
            };
        }

        /// <summary>
        /// Gets the feature set.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="featureSetType">Type of the feature set.</param>
        /// <returns>The <see cref="FeatureSet"/></returns>
        public static FeatureSet GetFeatureSet(User user, FeatureSetType featureSetType)
        {
            var features = new List<IFeature>();
            foreach (Type type in FeatureSets[featureSetType])
            {
                if (!user.FeatureCache.ContainsKey(type))
                {
                    IFeature feature = (IFeature)Activator.CreateInstance(type);
                    user.FeatureCache[type] = feature;

                    feature.Configure();

                    var configurable = feature as IConfigurableFeature;
                    if (configurable != null)
                    {
                        configurable.Configure(user);
                    }
                }
                    
                features.Add(user.FeatureCache[type]);
            }

            return new FeatureSet { Name = featureSetType.ToString(), Features = features.ToList() };
        }

        /// <summary>
        /// Gets the community set.
        /// </summary>
        /// <param name="featureSetType">Type of the feature set.</param>
        /// <returns>The <see cref="FeatureSet" />.</returns>
        public static FeatureSet GetCommunitySet(FeatureSetType featureSetType)
        {
            var features = new List<IFeature>();
            foreach (var type in FeatureSets[featureSetType])
            {
                if (type.IsImplementationOf(typeof(IConfigurableFeature)))
                {
                    continue;
                }

                var feature = (IFeature)Activator.CreateInstance(type);
                feature.Configure();

                features.Add(feature);
            }

            return new FeatureSet
                       {
                           Name = featureSetType.ToString(),
                           Features = features.ToList()
                       };
        }

        /// <summary>
        /// Gets the community set.
        /// </summary>
        /// <param name="communityFeatureSet">The community feature set.</param>
        /// <param name="user">The user.</param>
        /// <param name="featureSetType">Type of the feature set.</param>
        /// <returns>
        /// The <see cref="FeatureSet" />.
        /// </returns>
        public static FeatureSet GetPersonalSet(FeatureSet communityFeatureSet, User user, FeatureSetType featureSetType)
        {
            var personalFeatures = new List<IFeature>();

            foreach (Type type in FeatureSets[featureSetType].Where(type => type.IsImplementationOf(typeof(IConfigurableFeature))))
            {
                if (!user.FeatureCache.ContainsKey(type))
                {
                    var feature = (IFeature)Activator.CreateInstance(type);
                    user.FeatureCache[type] = feature;

                    feature.Configure();

                    var configurable = feature as IConfigurableFeature;
                    if (configurable != null)
                    {
                        configurable.Configure(user);
                    }
                }

                personalFeatures.Add(user.FeatureCache[type]);
            }

            return new FeatureSet
                       {
                           Name = featureSetType.ToString(),
                           Features = communityFeatureSet.Features.Concat(personalFeatures).ToList()
                       };
        }

        /// <summary>
        /// Computes the vector of binary features for a message, assuming it is a particular
        /// time after the message was received.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="message">The command.</param>
        /// <param name="includeSharedFeatures">if set to <c>true</c> [include shared features].</param>
        /// <returns>
        /// The <see cref="Dictionary{TKey, TValue}" />.
        /// </returns>
        public Dictionary<FeatureBucket, double> ComputeFeatureValues(User user, Message message, bool includeSharedFeatures)
        {
            var featureValues = new Dictionary<FeatureBucket, double>();

            foreach (var feature in this.Features)
            {
                if (!includeSharedFeatures && !feature.IsShared)
                {
                    continue;
                }

                var pair = new MessageFeaturePair { Message = message, Feature = feature };
                if (!user.FeatureBucketCache.ContainsKey(pair))
                {
                    user.FeatureBucketCache[pair] = feature.Compute(user, message);
                }

                foreach (var bucketValuePair in user.FeatureBucketCache[pair])
                {
                    featureValues[bucketValuePair.Bucket] = bucketValuePair.Value;
                }
            }

            return featureValues;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return new { this.DoNotInfer, this.Features, this.Name, this.Sparsity }.GetHashCode();
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(FeatureSet other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }
            
            if (!(this.DoNotInfer == other.DoNotInfer && this.Name == other.Name 
                && this.Sparsity == other.Sparsity))
            {
                return false;
            }

            if (this.Features.Count != other.Features.Count)
            {
                return false;
            }

            for (int i = 0; i < this.Features.Count; i++)
            {
                if (!((Feature)this.Features[i]).Equals((Feature)other.Features[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Features == null
                       ? string.Empty
                       : string.IsNullOrEmpty(this.Name) ? string.Join(",", this.Features.Select(ia => ia.GetType().Name)) : this.Name;
        }

        /// <summary>
        /// The message feature pair.
        /// </summary>
        public struct MessageFeaturePair
        {
            /// <summary>
            /// Gets or sets the message.
            /// </summary>
            public Message Message { get; set; }

            /// <summary>
            /// Gets or sets the feature.
            /// </summary>
            public IFeature Feature { get; set; }

            /// <summary>
            /// Returns a hash code for this instance.
            /// </summary>
            /// <returns>
            /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
            /// </returns>
            public override int GetHashCode()
            {
                return new { this.Message, this.Feature }.GetHashCode();
            }

            /// <summary>
            /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
            /// </summary>
            /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
            /// <returns>
            ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
            /// </returns>
            public override bool Equals(object obj)
            {
                var pair = obj as MessageFeaturePair?;
                return pair.HasValue && (this.Message.Equals(pair.Value.Message) && this.Feature.Equals(pair.Value.Feature));
            }
        }

        /// <summary>
        /// The feature exception.
        /// </summary>
        public class FeatureException : Exception
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="FeatureException"/> class.
            /// </summary>
            /// <param name="error">
            /// The error.
            /// </param>
            public FeatureException(string error)
                : base(error)
            {
            }
        }
    }
}