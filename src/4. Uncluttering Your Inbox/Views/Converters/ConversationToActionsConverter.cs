// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Views.Converters
{
    using System;
    using System.Globalization;


    using MBMLViews.Converters;

    /// <summary>
    /// The Conversation to actions converter.
    /// </summary>
    internal class ConversationToActionsConverter : BaseConverter<ConversationToActionsConverter>
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
            Conversation conversation = value as Conversation;
            return conversation == null ? string.Empty : string.Format("Reply={0:0.00}", conversation.ProbabilityOfReply);
        }
    }
}