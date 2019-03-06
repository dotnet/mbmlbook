// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLViews.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    /// <summary>
    /// The greater than to string converter.
    /// Converter that turns String and parameter to String or empty. Assumes that the output string is the first in the list
    /// </summary>
    [ValueConversion(typeof(String[]), typeof(String))]
    public class GreaterThanToStringConverter : BaseConverter<GreaterThanToStringConverter>
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
            string[] paramArray = parameter as string[];
            if (paramArray == null)
            {
                return string.Empty;
            }

            if (paramArray.Length != 2)
            {
                return string.Empty;
            }

            return System.Convert.ToInt32(value) > System.Convert.ToInt32(paramArray[1]) ? paramArray[0] : string.Empty;
        }
    }
}
