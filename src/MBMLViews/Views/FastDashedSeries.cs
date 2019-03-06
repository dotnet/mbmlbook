// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLViews.Views
{
    using System;
    using System.Windows.Media;
    using System.Windows.Shapes;

    using Microsoft.Research.Glo.Views;

    /// <summary>
    /// The fast dashed line series.
    /// </summary>
    /// <typeparam name="TIndependent">The type of the independent.</typeparam>
    /// <typeparam name="TDependent">The type of the dependent.</typeparam>
    public class FastDashedSeries<TIndependent, TDependent> : FastLineSeries<TIndependent, TDependent>
        where TIndependent : IComparable
        where TDependent : IComparable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FastDashedSeries{TIndependent,TDependent}"/> class.
        /// </summary>
        public FastDashedSeries()
        {
            // Some defaults
            this.StrokeDashArray = new[] { 5.0 };
            this.StrokeThickness = 2.0;
        }

        /// <summary>
        /// Gets or sets the stroke dash array.
        /// </summary>
        public double[] StrokeDashArray { get; set; }

        /// <summary>
        /// Gets or sets the stroke thickness.
        /// </summary>
        public double StrokeThickness { get; set; }

        /// <summary>
        /// The add shape from points.
        /// </summary>
        /// <param name="pts">The points.</param>
        /// <param name="maximum">The maximum.</param>
        protected override void AddShapeFromPoints(PointCollection pts, double maximum)
        {
            var pl = new Polyline
                         {
                             Points = pts,
                             StrokeThickness = this.StrokeThickness,
                             StrokeMiterLimit = 1,
                             Stroke = this.Background,
                             StrokeDashArray = new DoubleCollection(this.StrokeDashArray)
                         };
            this.Canvas.Children.Add(pl);
        }
    }
}