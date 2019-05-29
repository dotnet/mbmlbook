// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Views.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows.Media;

    using MBMLViews.Converters;

    /// <summary>
    /// Converts colors to brushes, blended with white according to the specified parameter.
    /// </summary>
    public class ColorToBrushConverter : BaseConverter<ColorToBrushConverter>
    {
        /// <summary>
        /// The brush cache.
        /// </summary>
        private static readonly Dictionary<Color, Brush> BrushCache = new Dictionary<Color, Brush>();

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
            Color? c = value as Color?;
            if (c == null)
            {
                return parameter != null ? Brushes.LightBlue : Brushes.Transparent;
            }

            float frac = 0.1f;
            if (parameter != null)
            {
                var ps = parameter as string;
                if (ps != null)
                {
                    try
                    {
                        frac = float.Parse(ps);
                    }
                    catch (FormatException)
                    {
                    }
                    catch (OverflowException)
                    {
                    }
                }
            }

            var blend = Blend(c.Value, Colors.White, frac);
            return GetBrush(blend);
        }

        /// <summary>
        /// Blends the specified colors.
        /// </summary>
        /// <param name="c1">The c1.</param>
        /// <param name="c2">The c2.</param>
        /// <param name="ratio">The ratio.</param>
        /// <returns>The <see cref="Color"/></returns>
        private static Color Blend(Color c1, Color c2, float ratio)
        {
            return Color.FromRgb(
                (byte)((c1.R * ratio) + (c2.R * (1 - ratio))),
                (byte)((c1.G * ratio) + (c2.G * (1 - ratio))),
                (byte)((c1.B * ratio) + (c2.B * (1 - ratio))));
        }

        /// <summary>
        /// Gets the brush.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns>The <see cref="Brush"/></returns>
        private static Brush GetBrush(Color color)
        {
            if (BrushCache.ContainsKey(color))
            {
                return BrushCache[color];
            }

            var brush = new SolidColorBrush(color);
            brush.Freeze();
            BrushCache[color] = brush;

            return BrushCache[color];
        }
    }
}