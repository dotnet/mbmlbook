// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLViews.Views
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    using Microsoft.Research.Glo.Views;

    /// <summary>
    /// The chart annotation.
    /// </summary>
    /// <typeparam name="TIndependent">The type of the independent.</typeparam>
    /// <typeparam name="TDependent">The type of the dependent.</typeparam>
    public class ChartAnnotation<TIndependent, TDependent> : FastLineSeries<TIndependent, TDependent>
        where TIndependent : IComparable
        where TDependent : IComparable
    {
        /// <summary>
        /// Gets or sets the annotation.
        /// </summary>
        public string Annotation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether show border.
        /// </summary>
        public bool ShowBorder { get; set; }

        /// <summary>
        /// Adds the shape from points.
        /// </summary>
        /// <param name="pts">The points.</param>
        /// <param name="maximum">The maximum.</param>
        protected override void AddShapeFromPoints(PointCollection pts, double maximum)
        {
            this.Background = Brushes.Transparent;
            foreach (var pt in pts)
            {
                var tb = this.ShowBorder
                             ? (FrameworkElement)
                               new TextBox
                                   {
                                       Text = this.Annotation,
                                       TextAlignment = TextAlignment.Center,
                                       HorizontalAlignment = HorizontalAlignment.Center,
                                       VerticalAlignment = VerticalAlignment.Center
                                   }
                             : new TextBlock
                                   {
                                       Text = this.Annotation,
                                       TextAlignment = TextAlignment.Center,
                                       HorizontalAlignment = HorizontalAlignment.Center,
                                       VerticalAlignment = VerticalAlignment.Center
                                   };

                this.Canvas.Children.Add(tb);
                tb.UpdateLayout();
                Canvas.SetLeft(tb, pt.X - (tb.ActualWidth / 2));
                Canvas.SetBottom(tb, pt.Y - (tb.ActualHeight / 2));
            }
        }
    }
}
