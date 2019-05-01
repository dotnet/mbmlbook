// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NETFULL
namespace MeetingYourMatch.Views
{
    using System;
    using System.Globalization;
    using System.Linq;

    using MBMLViews.Converters;

    /// <summary>
    /// Converts 0 to "Low" and 1 to "High"
    /// </summary>
    public class PerformanceValueConverter : BaseConverter<PerformanceValueConverter>
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
            ////string s = value as string;
            string p = parameter as string;
            if (p == null || !p.Contains(":"))
            {
                return string.Empty;
            }

            double[] par = p.Split(':').Select(double.Parse).ToArray();

            double val = value is string ? double.Parse((string)value) : value is double ? (double)value : double.NaN;
            
            if (par.Length != 2)
            {
                return string.Empty;
            }

            if (Math.Abs(val - par[0]) < double.Epsilon)
            {
                return "Low";
            }

            if (Math.Abs(val - par[1]) < double.Epsilon)
            {
                return "High";
            }

            return string.Empty;
        }
    }
}
#endif