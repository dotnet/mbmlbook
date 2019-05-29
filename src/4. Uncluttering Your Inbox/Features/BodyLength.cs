// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;


    /// <summary>
    /// The Body Character Length feature.
    /// </summary>
    [Serializable]
    public class BodyLength : NumericFeature
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BodyLength"/> class.
        /// </summary>
        public BodyLength() : this(FeatureSet.BodyCharLengthBins)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyLength"/> class.
        /// </summary>
        /// <param name="bins">
        /// The bins.
        /// </param>
        public BodyLength(int[] bins)
            : base(bins)
        {
            this.Description = "The number of new characters in the body text";
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
            foreach (var bucket in this.Buckets)
            {
                if (message.NewText.Length <= bucket.Item)
                {
                    return bucket.Index;
                }
            }

            return val;
        }
    }
}
