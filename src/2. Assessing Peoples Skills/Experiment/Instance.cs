// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AssessingPeoplesSkills
{
    using System;

    /// <summary>
    /// The instance. Needed for Infer.NET metrics only
    /// </summary>
    public class Instance : IComparable<Instance>
    {
        /// <summary>
        /// Gets or sets a value indicating whether the ground truth (measurement) is correct.
        /// </summary>
        public bool Measurement { get; set; }

        /// <summary>
        /// Gets or sets the prediction.
        /// </summary>
        public double Prediction { get; set; }

        /// <summary>
        /// Gets or sets the index. Used for ordering and equality checking
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <param name="first">The first instance.</param>
        /// <param name="second">The second instance.</param>
        /// <returns>A value that indicates whether these are the same instance.</returns>
        public static bool operator ==(Instance first, Instance second)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(first, second))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)first == null) || ((object)second == null))
            {
                return false;
            }

            return first.Index == second.Index;
        }

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <param name="first">The first instance.</param>
        /// <param name="second">The second instance.</param>
        /// <returns>A value that indicates whether these are not the same instance.</returns>
        public static bool operator !=(Instance first, Instance second)
        {
            return !(first == second);
        }

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>A value that indicates whether these are the same instance.</returns>
        public bool Equals(Instance other)
        {
            return this.Measurement.Equals(other.Measurement) && this.Prediction.Equals(other.Prediction) && this.Index == other.Index;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/>, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == this.GetType() && this.Equals((Instance)obj);
        }

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other" /> parameter.Zero This object is equal to <paramref name="other" />. Greater than zero This object is greater than <paramref name="other" />.
        /// </returns>
        public int CompareTo(Instance other)
        {
            return this.Index.CompareTo(other.Index);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = this.Measurement.GetHashCode();
                hashCode = (hashCode * 397) ^ this.Prediction.GetHashCode();
                hashCode = (hashCode * 397) ^ this.Index;
                return hashCode;
            }
        }
    }
}