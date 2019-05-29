// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{

    /// <summary>
    /// The from manager. Dummy feature that always returns false. Used in feature testing only
    /// </summary>
    public class FromManager : BinaryFeature
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FromManager"/> class.
        /// </summary>
        public FromManager()
        {
            this.Description = "Whether the message is from your manager";
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
            return false;
        }
    }
}
