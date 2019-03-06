// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLViews.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    using MBMLViews.Views;

    /// <summary>
    /// The boolean to integer converter. true -> parameter, false -> 0
    /// </summary>
    [ValueConversion(typeof(bool), typeof(int))]
    public class BooleanToIntConverter : BaseConverter<BooleanToIntConverter>
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
            return (bool)value ? int.Parse((string)parameter) : 0;
        }
    }
}
