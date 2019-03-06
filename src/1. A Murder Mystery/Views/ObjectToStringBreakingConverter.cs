// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MurderMystery
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    using Microsoft.Research.Glo.Views.Converters;

    /// <summary>
    /// The object to string converter. Breaks words if parameter is set to true.
    /// </summary>
    [ValueConversion(typeof(object), typeof(string))]
    internal class ObjectToStringBreakingConverter : BaseConverter, IValueConverter
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
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null 
                ? string.Empty
                : parameter != null && bool.Parse((string)parameter)
                    ? Microsoft.Research.Glo.ObjectToStringConverter.BreakWords(value.ToString()) 
                    : value.ToString();
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <exception cref="System.NotImplementedException">Reverse conversion not supported.</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}