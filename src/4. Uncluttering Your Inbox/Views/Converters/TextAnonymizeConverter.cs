// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Views.Converters
{
    using System;
    using System.Globalization;

    using UnclutteringYourInbox.DataCleaning;

    using Microsoft.Research.Glo;

    using MBMLViews.Converters;

    /// <summary>
    /// The text obfuscation converter.
    /// </summary>
    public class TextAnonymizeConverter : BaseMultiConverter<TextAnonymizeConverter>
    {
        /// <summary>
        /// Converts the specified values.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>The anonymized text.</returns>
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2)
            {
                return null;
            }

            string text = values[0] as string;
            var anonymize = values[1] is Anonymize ? (Anonymize)values[1] : Anonymize.DoNotAnonymize;

            if (text == null)
            {
                text = ObjectToStringConverter.ToDisplayString(values[0]);
            }

            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            if (anonymize == Anonymize.DoNotAnonymize)
            {
                return text;
            }

            return Anonymizer.WordsToX(text);
        }
    }
}
