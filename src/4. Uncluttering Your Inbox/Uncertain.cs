// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox
{
    using System;

    /// <summary>
    /// An uncertain instance of type T.
    /// </summary>
    /// <typeparam name="T">Type T</typeparam>
    [Serializable]
    public struct Uncertain<T> : IComparable
    {
        /// <summary>
        /// Gets a value indicating whether this instance is certain.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is certain; otherwise, <c>false</c>.
        /// </value>
        public bool IsCertain
        {
            get
            {
                return Math.Abs(this.Probability - 1.0) < double.Epsilon;
            }
        }

        /// <summary>
        /// Gets or sets the probability.
        /// </summary>
        public double Probability { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// Returns and uncertain version the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public static implicit operator Uncertain<T>(T item)
        {
            return new Uncertain<T> { Probability = 1.0, Value = item };
        }

        /// <summary>
        /// Returns and uncertain version the specified item with probability.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="prob">The probability.</param>
        /// <returns>The uncertain value</returns>
        public static Uncertain<T> FromProb(T value, double prob)
        {
            return new Uncertain<T> { Value = value, Probability = prob };
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            string s = string.Empty + this.Value;
            if (!this.IsCertain)
            {
                s += "?";
            }

            return Math.Abs(this.Probability - 0) < double.Epsilon ? string.Empty : s;
        }

        #region IComparable Members

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="obj" /> in the sort order. Zero This instance occurs in the same position in the sort order as <paramref name="obj" />. Greater than zero This instance follows <paramref name="obj" /> in the sort order.
        /// </returns>
        public int CompareTo(object obj)
        {
            if (!(obj is Uncertain<T>))
            {
                return 0;
            }

            var unc = (Uncertain<T>)obj;
            IComparable value = this.Value as IComparable;
            if (value != null)
            {
                return value.CompareTo(unc.Value);
            }

            IComparable comparable = unc.Value as IComparable;
            if (comparable != null)
            {
                return -comparable.CompareTo(this.Value);
            }

            return 0;
        }

        #endregion

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return ReferenceEquals(this.Value, null) ? 0 : this.Value.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Uncertain<T>))
            {
                return false;
            }

            Uncertain<T> unc = (Uncertain<T>)obj;
            if (ReferenceEquals(this.Value, null))
            {
                return ReferenceEquals(unc.Value, null);
            }

            return this.Value.Equals(unc.Value);
        }
    }
}
