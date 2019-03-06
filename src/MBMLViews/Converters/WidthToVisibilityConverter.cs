// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLViews.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    /// The width to visibility converter. Width less than Parameter -> Collapsed, otherwise visible
    /// </summary>
    [ValueConversion(typeof(int), typeof(Visibility))]
    public class WidthToVisibilityConverter : BaseConverter<WidthToVisibilityConverter>
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
            if (value == null)
            {
                return Visibility.Visible;
            }

            return System.Convert.ToInt32(value) < System.Convert.ToInt32(parameter) ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
