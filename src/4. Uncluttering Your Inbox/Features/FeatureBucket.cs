// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;

    /// <summary>
    /// The feature bucket.
    /// </summary>
    public class FeatureBucket : IEquatable<FeatureBucket>
    {
        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        public dynamic Item { get; set; }

        /// <summary>
        /// Gets or sets the parent feature for this bucket.
        /// </summary>
        public IFeature Feature { get; set; }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(FeatureBucket other)
        {
            return this.Name == other.Name && this.Index == other.Index;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            // Use anonymous type to generate hash code
            var featureName = this.Feature.Name;
            return new { this.Name, this.Index, featureName }.GetHashCode();
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
            var bucket = obj as FeatureBucket;
            return bucket != null && this.Equals(bucket);
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public sealed override string ToString()
        {
            return this.Feature is BinaryFeature || this.Feature is Bias
                       ? this.Feature.Name
                       : string.Format("{0}[{1}]", this.Feature.Name, this.Name);
        }
    }
}