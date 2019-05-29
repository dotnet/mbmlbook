// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;
    using System.Linq;


    /// <summary>
    /// The you are on the to line or Cc line feature.
    /// </summary>
    [Serializable]
    public class ToCcLine : BinaryFeature
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToCcLine" /> class.
        /// </summary>
        public ToCcLine()
        {
            this.Description = "Whether or not you are on the To line or Cc line";
        }

        /// <summary>
        /// Computes the feature.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>
        /// The <see cref="System.Boolean" />
        /// </returns>
        public override bool ComputeFeature(Message message)
        {
            return message.SentTo.Any(cd => cd.IsMe) || message.CopiedTo.Any(cd => cd.IsMe);
        }
    }
}
