// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Views.Converters
{
    using System;
    using System.Globalization;


    using MBMLViews.Converters;

    /// <summary>
    /// The message to expansion state converter.
    /// </summary>
    public class MessageToExpansionStateConverter : BaseConverter<MessageToExpansionStateConverter>
    {
        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Message m = (Message)value;
            if ((m == null) || (m.Conversation == null))
            {
                return false;
            }

            int k = m.Conversation.Messages.IndexOf(m);

            // expand message if unread, flagged or latest message.
            bool expand = m.ProbabilityOfReply > 0.5;

            // also expand if likely to reply to the message
            if (m.Sender.IsMe)
            {
                expand = false;
            }

            if ((!m.IsRead) || (m.Flag == FlagState.Flagged) || (k == m.Conversation.Messages.Count - 1))
            {
                expand = true;
            }

            if (m.IsDeleted)
            {
                expand = false;
            }

            return expand;
        }
    }
}