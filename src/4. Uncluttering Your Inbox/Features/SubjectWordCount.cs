// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;


    /// <summary>
    /// The Subject Word Count feature.
    /// </summary>
    [Serializable]
    public class SubjectWordCount : NumericFeature
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubjectWordCount"/> class.
        /// </summary>
        public SubjectWordCount() : this(FeatureSet.SubjectWordCountBins)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubjectWordCount" /> class.
        /// </summary>
        /// <param name="bins">The bins.</param>
        public SubjectWordCount(int[] bins) : base(bins)
        {
            this.Description = "The number of words in the subject";
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
                if (message.SubjectWords.Count > this.Buckets[i].Item)
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
