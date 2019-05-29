// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;


    /// <summary>
    /// Base class for creating features
    /// </summary>
    public abstract class Feature : IFeature, IEquatable<Feature>
    {
        /// <summary>
        /// Whether this feature is shared between users.
        /// </summary>
        private bool isShared = true;

        /// <summary>
        /// The string format to produce a short description.
        /// </summary>
        private string stringFormat = "{0}";

        /// <summary>
        /// The base type name.
        /// </summary>
        private string baseTypeName;

        /// <summary>
        /// The name.
        /// </summary>
        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="Feature"/> class.
        /// </summary>
        protected Feature()
        {
            this.Buckets = new List<FeatureBucket>();
        }

        /// <summary>
        /// Gets or sets the buckets. 
        /// These are created by the constructors of the derived classes,
        /// so don't need to be serialized.
        /// </summary>
        public List<FeatureBucket> Buckets { get; set; }

        /// <summary>
        /// Gets the count.
        /// </summary>
        public int Count
        {
            get
            {
                return this.Buckets == null ? 0 : this.Buckets.Count;
            }
        }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the string format to produce a short description.
        /// </summary>
        public string StringFormat
        {
            get
            {
                return this.stringFormat;
            }

            set
            {
                this.stringFormat = value;
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public virtual string Name
        {
            get
            {
                return this.name ?? (this.name = GetType().Name);
            }
        }

        /// <summary>
        /// Gets or sets the first description.
        /// </summary>
        public string FirstDescription { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is shared.
        /// </summary>
        public bool IsShared
        {
            get
            {
                return this.isShared;
            }

            set
            {
                this.isShared = value;
            }
        }

        /// <summary>
        /// Gets the feature names.
        /// </summary>
        public string[] FeatureNames
        {
            get
            {
                return this.Buckets == null ? null : this.Buckets.Select(ia => ia.Name).ToArray();
            }
        }

        /// <summary>
        /// Gets the base type name.
        /// </summary>
        public string BaseTypeName
        {
            get
            {
                if (this.baseTypeName != null)
                {
                    return this.baseTypeName;
                }

                var baseType = this.GetType().BaseType;
                this.baseTypeName = baseType != null ? baseType.Name.Split('`')[0] : string.Empty;

                return this.baseTypeName;
            }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public string GetDescription(int i)
        {
            if ((i == 0) && (this.FirstDescription != null))
            {
                return this.FirstDescription;
            }

            if ((this.Count > 1) && (this.FeatureNames.Length > 1))
            {
                return string.Format(this.StringFormat, this.FeatureNames[i]);
            }

            return this.StringFormat;
        }

        /// <summary>
        /// Configure the feature.
        /// </summary>
        public abstract void Configure();

        /// <summary>
        /// Computes the feature.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="message">The message.</param>
        /// <returns>
        /// The active buckets with the values
        /// </returns>
        public abstract IList<FeatureBucketValuePair> Compute(User user, Message message);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public virtual int GetHashCode()
        {
            return new { this.Buckets, this.Description, this.FirstDescription, this.IsShared, this.StringFormat }.GetHashCode();
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public virtual bool Equals(Feature other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (
                !(this.IsShared == other.IsShared && this.Description == other.Description
                  && this.FirstDescription == other.FirstDescription && this.StringFormat == other.StringFormat))
            {
                return false;
            }
            
            if (this.Buckets.Count != other.Buckets.Count)
            {
                return false;
            }

            for (int i = 0; i < this.Buckets.Count; i++)
            {
                if (!this.Buckets[i].Equals(other.Buckets[i]))
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
            return this.Description;
        }
    }
}