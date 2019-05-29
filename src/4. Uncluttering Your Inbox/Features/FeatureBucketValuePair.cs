// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    /// <summary>
    /// The feature bucket value pair.
    /// </summary>
    public struct FeatureBucketValuePair
    {
        /// <summary>
        /// Gets or sets the feature bucket.
        /// </summary>
        public FeatureBucket Bucket { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See <a href="http://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations" />
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + this.Bucket.GetHashCode();
                hash = (hash * 31) + this.Value.GetHashCode();
                return hash;
            }
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
            var pair = obj as FeatureBucketValuePair?;
            return pair.HasValue && (this.Bucket.Equals(pair.Value.Bucket) && (this.Value - pair.Value.Value < double.Epsilon));
        }
    }
}