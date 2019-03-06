// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLViews.Views
{
    using System;
    using System.Windows;
    using System.Windows.Media;

    using Microsoft.Research.Glo.Views;
    
    /// <summary>
    /// The fast arrow series.
    /// </summary>
    /// <typeparam name="TIndependent">The type of the independent.</typeparam>
    /// <typeparam name="TDependent">The type of the dependent.</typeparam>
    public class FastArrowSeries<TIndependent, TDependent> : FastLineSeries<TIndependent, TDependent>
        where TIndependent : IComparable
        where TDependent : IComparable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FastArrowSeries{TIndependent, TDependent}"/> class.
        /// </summary>
        public FastArrowSeries()
        {
            // Some sensible defaults
            this.ArrowWidth = 2;
            this.ArrowLength = 5;
            this.StrokeThickness = 2;
            this.BorderBrush = Brushes.Black;
            this.IsStartArrow = false;
            this.IsEndArrow = true;
        }

        /// <summary>
        /// Gets or sets the arrow width.
        /// </summary>
        public double ArrowWidth { get; set; }

        /// <summary>
        /// Gets or sets the arrow length.
        /// </summary>
        public double ArrowLength { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is end arrow.
        /// </summary>
        public bool IsEndArrow { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is start arrow.
        /// </summary>
        public bool IsStartArrow { get; set; }

        /// <summary>
        /// Gets or sets the stroke thickness.
        /// </summary>
        public double StrokeThickness { get; set; }

        /// <summary>
        /// Adds the shape from points.
        /// </summary>
        /// <param name="pts">The points.</param>
        /// <param name="maximum">The maximum.</param>
        protected override void AddShapeFromPoints(PointCollection pts, double maximum)
        {
            this.Background = Brushes.Transparent;
            int cnt = pts.Count / 2;
            if (cnt == 0)
            {
                return;
            }

            for (int i = 0; i < cnt; i++)
            {
                var pt1 = pts[i * 2];
                var pt2 = pts[(i * 2) + 1];

                var arrow = new Arrow
                                {
                                    X1 = pt1.X,
                                    Y1 = pt1.Y,
                                    X2 = pt2.X,
                                    Y2 = pt2.Y,
                                    Stroke = Brushes.Black,
                                    StrokeThickness = this.StrokeThickness,
                                    IsEndArrow = this.IsEndArrow,
                                    IsStartArrow = this.IsStartArrow,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    ArrowLength = this.ArrowLength,
                                    ArrowWidth = this.ArrowWidth
                                };
            
                this.Canvas.Children.Add(arrow);
            }
        }
    }
}
