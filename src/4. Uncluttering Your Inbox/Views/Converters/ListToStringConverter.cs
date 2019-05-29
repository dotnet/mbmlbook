// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Views.Converters
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Text;

    using Microsoft.Research.Glo;

    using MBMLViews.Converters;

    /// <summary>
    /// The list to string converter.
    /// </summary>
    internal class ListToStringConverter : BaseConverter<ListToStringConverter>
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
            IList l = value as IList;
            if (l == null)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            int ct = 0;
            foreach (object obj in l)
            {
                if (ct > 0)
                {
                    sb.Append(", ");
                }

                ct++;
                sb.Append(ObjectToStringConverter.ToDisplayString(obj));
            }

            return sb.ToString();
        }
    }
}