// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;


    /// <summary>
    /// The appears to be an automated sender feature.
    /// </summary>
    [Serializable]
    public class IsAutomatedSender : BinaryFeature
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IsAutomatedSender"/> class. 
        /// </summary>
        public IsAutomatedSender()
        {
            this.Description = "Whether the message appears to be from an automated sender";
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
            string email = message.Sender.Email.ToString().ToLowerInvariant(); 
            string name = message.Sender.Name.ToString().ToLowerInvariant(); 
            
            return email.Contains("noreply") || email.Contains("no-reply") || email.Contains("notreply") || email.Contains("not-reply")
                   || email.Contains("auto") || name.Contains("noreply") || name.Contains("no-reply") || name.Contains("notreply")
                   || name.Contains("not-reply") || name.Contains("auto");
        }
    }
}
