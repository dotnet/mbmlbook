// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLViews
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    using SWMBrushes = System.Windows.Media.Brushes;
    using SWMColors = System.Windows.Media.Colors;

    /// <summary>
    /// The color map order.
    /// </summary>
    public enum ColorMapOrder
    {
        /// <summary>
        /// normal ordering.
        /// </summary>
        Normal,

        /// <summary>
        /// reversed ordering.
        /// </summary>
        Reversed
    }

    /// <summary>
    /// The color map.
    /// </summary>
    [LocalizabilityAttribute(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public enum ColorMap
    {
        /// <summary>
        /// grayscale color map
        /// </summary>
        Gray = 0,

        /// <summary>
        /// Rainbow like custom color map with 7 colors.
        /// </summary>
        Rainbow = 1,

        /// <summary>
        /// gray color map buffered so that pure black isn't used
        /// </summary>
        GrayBuffered = 2,
    }

    /// <summary>
    /// The color map brush container.
    /// </summary>
    public class ColorMapBrushContainer
    {
        /// <summary>
        /// The color map.
        /// </summary>
        private readonly ColorMap colorMap;

        /// <summary>
        /// The data range
        /// </summary>
        private readonly RealRange range;

        /// <summary>
        /// The y increment.
        /// </summary>
        private readonly double yIncrement;

        /// <summary>
        /// The brush cache.
        /// </summary>
        private readonly Dictionary<Color, SolidColorBrush> brushCache = new Dictionary<Color, SolidColorBrush>();

        /// <summary>
        /// The brushes.
        /// </summary>
        private SolidColorBrush[] brushes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorMapBrushContainer" /> class.
        /// </summary>
        /// <param name="colorMap">The color map.</param>
        /// <param name="colorMapLength">Length of the HSV color map.</param>
        public ColorMapBrushContainer(ColorMap colorMap, int colorMapLength = -1)
            : this(colorMap, new RealRange { Min = 0.0, Max = 1.0 }, colorMapLength)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorMapBrushContainer" /> class.
        /// </summary>
        /// <param name="colorMap">The color map.</param>
        /// <param name="range">The range.</param>
        /// <param name="colorMapLength">Length of the HSV color map.</param>
        /// <exception cref="System.ArgumentException">HSV color map requires length &gt; 1; colorMapLength</exception>
        public ColorMapBrushContainer(ColorMap colorMap, RealRange range, int colorMapLength = -1)
        {
            this.colorMap = colorMap;
            this.range = range;
            this.SetColorMapBrushes(colorMapLength);
            this.yIncrement = range.Delta / (this.brushes.Length - 1);
        }

        /// <summary>
        /// Gets the colors.
        /// </summary>
        public Color[] Colors { get; private set; }

        /// <summary>
        /// Gets the end color.
        /// </summary>
        public Color EndColor
        {
            get
            {
                return this.Colors.Last();
            }
        }

        /// <summary>
        /// Gets the start color.
        /// </summary>
        public Color StartColor
        {
            get
            {
                return this.Colors.First();
            }
        }

        /// <summary>
        /// Gets the quantiles.
        /// </summary>
        public double[] Quantiles
        {
            get
            {
                double[] quantiles = new double[this.brushes.Length];
                for (int i = 0; i < this.brushes.Length; i++)
                {
                    quantiles[i] = this.range.Min + (i * this.yIncrement);
                }

                return quantiles;
            }
       }

        /// <summary>
        /// Gets the cache size.
        /// </summary>
        public int CacheSize
        {
            get
            {
                return this.brushCache.Count;
            }
        }

        /// <summary>
        /// Gets or sets the color map order.
        /// </summary>
        public ColorMapOrder ColorMapOrder { get; set; }

        /// <summary>
        /// Creates a gradient brush from the colors.
        /// </summary>
        /// <param name="orientation">The brush orientation</param>
        /// <returns>The <see cref="LinearGradientBrush"/>.</returns>
        public LinearGradientBrush GetGradientBrush(Orientation orientation)
        {
            LinearGradientBrush brush = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = orientation == Orientation.Horizontal ? new Point(1, 0) : new Point(0, 1)
                    };

            for (int i = 0; i < this.Colors.Length; i++)
            {
                brush.GradientStops.Add(new GradientStop(this.Colors[i], (double)i / (this.Colors.Length - 1)));
            }

            return brush;
        }
       
        /// <summary>
        /// Gets the brush.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>
        /// "The SolidColorBrush."
        /// </returns>
        public SolidColorBrush GetBrush(int index)
        {
            return this.brushes[index % this.brushes.Length];
        }

        /// <summary>
        /// Gets the brush.
        /// </summary>
        /// <param name="y">The y value.</param>
        /// <returns>The brush.</returns>
        public SolidColorBrush GetBrush(double y)
        {
            if (y <= this.range.Min)
            {
                return this.ColorMapOrder == ColorMapOrder.Normal ? this.brushes.First() : this.brushes.Last();
            }

            if (y >= this.range.Max)
            {
                return this.ColorMapOrder == ColorMapOrder.Normal ? this.brushes.Last() : this.brushes.First();
            }

            if (this.ColorMapOrder == ColorMapOrder.Reversed)
            {
                y = this.range.Max - y + this.range.Min;
            }

            double n = (y - this.range.Min) / this.yIncrement;

            int q = (int)Math.Floor(n);
                
            Color c = Interpolate(this.Colors[q], this.Colors[q + 1], n);

            if (!this.brushCache.ContainsKey(c))
            {
                this.brushCache[c] = new SolidColorBrush(c);
            }

            return this.brushCache[c];

            ////return new SolidColorBrush(ColorHelper.Interpolate(this.Colors[q], this.Colors[q + 1], n));
        }

        /// <summary>
        /// Interpolates between the specified colors.
        /// </summary>
        /// <param name="first">The first color.</param>
        /// <param name="second">The second color.</param>
        /// <param name="y">The y.</param>
        /// <returns>The <see cref="Color"/></returns>
        private static Color Interpolate(Color first, Color second, double y)
        {
            double r = (first.R * (1 - y)) + (second.R * y);
            double g = (first.G * (1 - y)) + (second.G * y);
            double b = (first.B * (1 - y)) + (second.B * y);
            double a = (first.A * (1 - y)) + (second.A * y);
            return Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
        }

        /// <summary>
        /// Color from Hue Saturation Variance.
        /// See http://en.wikipedia.org/wiki/HSL_color_space
        /// </summary>
        /// <param name="hue">The hue.</param>
        /// <param name="saturation">The saturation.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The color.
        /// </returns>
        private static Color ColorFromHsv(double hue, double saturation, double value)
        {
            int hueBoxed = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = (hue / 60) - Math.Floor(hue / 60);

            value *= 255;
            byte v = Convert.ToByte(value);
            byte p = Convert.ToByte(value * (1 - saturation));
            byte q = Convert.ToByte(value * (1 - (f * saturation)));
            byte t = Convert.ToByte(value * (1 - ((1 - f) * saturation)));

            switch (hueBoxed)
            {
                case 0: return Color.FromArgb(255, v, t, p);
                case 1: return Color.FromArgb(255, q, v, p);
                case 2: return Color.FromArgb(255, p, v, t);
                case 3: return Color.FromArgb(255, p, q, v);
                case 4: return Color.FromArgb(255, t, p, v);
            }

            return Color.FromArgb(255, v, p, q);
        }

        /// <summary>
        /// Sets the color map brushes.
        /// </summary>
        /// <param name="colorMapLength">Length of the color map.</param>
        /// <exception cref="System.Exception">Unknown color map:  + colorMap.ToString()</exception>
        private void SetColorMapBrushes(int colorMapLength)
        {
            switch (this.colorMap)
            {
                case ColorMap.Gray:
                    this.Colors = new[] { SWMColors.Black, SWMColors.White };
                    break;
                case ColorMap.GrayBuffered:
                    this.Colors = new[] { Color.FromRgb(50, 50, 50), SWMColors.White };
                    break;
                case ColorMap.Rainbow:
                    this.Colors = new[] 
                                        { 
                                            SWMColors.Red,
                                            SWMColors.Orange,
                                            SWMColors.Yellow,
                                            SWMColors.LimeGreen,
                                            SWMColors.Cyan,
                                            SWMColors.Blue,
                                            SWMColors.Magenta
                                        };
                    break;
                default:
                    throw new Exception("Unknown colormap: " + this.colorMap);
            }

            this.brushes = this.Colors.Select(color => new SolidColorBrush(color)).ToArray();
        }
    }
}
