// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;


    /// <summary>
    /// The Subject Character Length feature.
    /// </summary>
    [Serializable]
    public class SubjectLength : NumericFeature
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubjectLength"/> class. 
        /// </summary>
        public SubjectLength() : this(FeatureSet.SubjectCharLengthBins)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubjectLength"/> class. 
        /// </summary>
        /// <param name="bins">
        /// The bins.
        /// </param>
        public SubjectLength(int[] bins) : base(bins)
        {
            this.Description = "The number of characters in the subject";
            this.StringFormat = "{0}";
        }

        /// <summary>
        /// Computes the feature.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>
        /// The index of the feature that is on.
        /// </returns>
        public override int ComputeFeature(Message message)
        {
            int val = this.Buckets.Count - 1;
            for (int i = 0; i < this.Buckets.Count; i++)
            {
                if (message.SubjectWithoutPrefix.Length > this.Buckets[i].Item)
                {
                    continue;
                }

                val = i;
                break;
            }

            return val;
        }
    }
}
