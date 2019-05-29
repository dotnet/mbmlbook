// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Views.Converters
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows;

    using MBMLViews.Converters;

    /// <summary>
    /// The cutoff converter.
    /// </summary>
    public class CutoffConverter : BaseMultiConverter<CutoffConverter>
    {
        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="values">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2)
            {
                throw new InvalidOperationException();
            }

            return values[1] != DependencyProperty.UnsetValue && (double)values[0] > (double)values[1];
        }
    }
}