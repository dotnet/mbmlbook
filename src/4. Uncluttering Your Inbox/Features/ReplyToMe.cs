// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;
    using System.Linq;


    /// <summary>
    /// The is reply to message from me feature.
    /// </summary>
    [Serializable]
    public class ReplyToMe : BinaryFeature
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReplyToMe"/> class. 
        /// </summary>
        public ReplyToMe()
        {
            this.Description = "Whether this message is a reply to an earlier message from you";
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
            var conversationMessages = message.Conversation.Messages;
            int cidx = conversationMessages.IndexOf(message);
            return !message.Sender.IsMe &&
                                 conversationMessages.Take(cidx)
                                 .Any(msg => msg.Sender.IsMe && msg.Recipients.Contains(message.Sender.Person));
        }
    }
}
