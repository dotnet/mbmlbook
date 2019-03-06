// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLViews.Views
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.DataVisualization.Charting;
    using System.Windows.Media;
    using System.Windows.Shapes;

    using Microsoft.Research.Glo.Views;

    /// <summary>
    /// A base class that contains methods used by both the line and area series.
    /// </summary>
    /// <typeparam name="TIndependent">The type of the independent.</typeparam>
    /// <typeparam name="TDependent">The type of the dependent.</typeparam>
    public class FastAnnotatedScatterSeries<TIndependent, TDependent> : FastSingleSeriesWithAxes<TIndependent, TDependent> 
        where TIndependent : IComparable
        where TDependent : IComparable
    {
        /// <summary>
        /// Identifies the DependentRangeAxis dependency property.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes", Justification = "This member is necessary because child classes need to share this dependency property.")]
        public static readonly DependencyProperty DependentRangeAxisProperty =
            DependencyProperty.Register(
                "DependentRangeAxis",
                typeof(IRangeAxis),
                typeof(FastAnnotatedScatterSeries<TIndependent, TDependent>),
                new PropertyMetadata(null, OnDependentRangeAxisPropertyChanged));

        /// <summary>
        /// Identifies the IndependentAxis dependency property.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes", Justification = "This member is necessary because child classes need to share this dependency property.")]
        public static readonly DependencyProperty IndependentAxisProperty =
            DependencyProperty.Register(
                "IndependentAxis",
                typeof(IAxis),
                typeof(FastAnnotatedScatterSeries<TIndependent, TDependent>),
                new PropertyMetadata(null, OnIndependentAxisPropertyChanged));

        /// <summary>
        /// Gets or sets the dependent range axis.
        /// </summary>
        public IRangeAxis DependentRangeAxis
        {
            get { return this.GetValue(DependentRangeAxisProperty) as IRangeAxis; }
            set { this.SetValue(DependentRangeAxisProperty, value); }
        }

        /// <summary>
        /// Gets or sets the independent range axis.
        /// </summary>
        public IAxis IndependentAxis
        {
            get { return this.GetValue(IndependentAxisProperty) as IAxis; }
            set { this.SetValue(IndependentAxisProperty, value); }
        }

        /// <summary>
        /// Gets the independent axis as a range axis.
        /// </summary>
        public IAxis ActualIndependentAxis
        {
            get
            {
                return this.InternalActualIndependentAxis;
            }
        }

        /// <summary>
        /// Gets the dependent axis as a range axis.
        /// </summary>
        public IRangeAxis ActualDependentRangeAxis
        {
            get
            {
                return this.InternalActualDependentAxis as IRangeAxis;
            }
        }

        /// <summary>
        /// Gets or sets the line marker.
        /// </summary>
        public MarkerType LineMarker { get; set; }

        /// <summary>
        /// Gets or sets the marker size.
        /// </summary>
        public double MarkerSize { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        public string TextFormat { get; set; }

        /// <summary>
        /// Gets or sets the offset x.
        /// </summary>
        public double OffsetX { get; set; }

        /// <summary>
        /// Gets or sets the offset y.
        /// </summary>
        public double OffsetY { get; set; }

        /// <summary>
        /// Gets or sets the annotation brush.
        /// </summary>
        public Brush AnnotationBrush { get; set; }

        /// <summary>
        /// Acquire a horizontal linear axis and a vertical linear axis.
        /// </summary>
        /// <param name="firstPoint">The first point.</param>
        protected override void GetAxes(ChartPoint<TIndependent, TDependent> firstPoint)
        {
            this.GetAxes(
                firstPoint,
                axis => axis.Orientation == AxisOrientation.X,
                () =>
                {
                    IAxis axis = CreateRangeAxisFromType(typeof(TIndependent)) ?? (IAxis)new CategoryAxis();
                    axis.Orientation = AxisOrientation.X;
                    return axis;
                },
                axis => axis.Orientation == AxisOrientation.Y && axis is IRangeAxis,
                () =>
                {
                    DisplayAxis axis = (DisplayAxis)CreateRangeAxisFromType(typeof(TDependent));
                    if (axis == null)
                    {
                        throw new InvalidOperationException("No suitable axis found");
                    }
                    axis.ShowGridLines = true;
                    axis.Orientation = AxisOrientation.Y;
                    return axis;
                });
        }


        /// <summary>
        /// Returns the custom ResourceDictionary to use for necessary resources.
        /// </summary>
        /// <returns>
        /// ResourceDictionary to use for necessary resources.
        /// </returns>
        protected override IEnumerator<ResourceDictionary> GetResourceDictionaryEnumeratorFromHost()
        {
            return this.SeriesHost.GetResourceDictionariesWhere(dictionary =>
            {
                Style style = dictionary[DataPointStyleName] as Style;
                if (null != style)
                {
                    return (null != style.TargetType) &&
                           ((typeof(DataPoint) == style.TargetType) || style.TargetType.IsAssignableFrom(typeof(DataPoint)));
                }
                return false;
            });
        }

        /// <summary>
        /// Builds the data points.
        /// </summary>
        protected override void BuildDataPoints()
        {
            double maximum = this.ActualDependentRangeAxis.GetPlotAreaCoordinate(this.ActualDependentRangeAxis.Range.Maximum).Value;
            if (!FastSeries.CanGraph(maximum))
            {
                return;
            }

            var pcs = this.GetPointCollections(maximum, this.points, pt => pt.Y);
            foreach (var pc in pcs)
            {
                this.AddShapeFromPoints(pc, maximum);
                this.AddMarkers(pc);
            }

            if (this.pointsWithBounds == null)
            {
                return;
            }

            var upperpcs = this.GetPointCollections(maximum, this.pointsWithBounds, pt => pt.Upper);
            var lowerpcs = this.GetPointCollections(maximum, this.pointsWithBounds, pt => pt.Lower);
            for (int i = 0; i < lowerpcs.Count; i++)
            {
                this.AddBounds(lowerpcs[i], upperpcs[i]);
            }
        }

        /// <summary>
        /// Adds the bounds.
        /// </summary>
        /// <param name="lowerPoints">The lower points.</param>
        /// <param name="upperPoints">The upper points.</param>
        protected virtual void AddBounds(PointCollection lowerPoints, PointCollection upperPoints)
        {
        }

        /// <summary>
        /// Updates the Series shape object from a collection of Points.
        /// </summary>
        /// <param name="pts">Collection of Points.</param>
        /// <param name="maximum">The maximum.</param>
        protected virtual void AddShapeFromPoints(PointCollection pts, double maximum)
        {
            // overridden to provide line and area charts
        }

        /// <summary>
        /// DependentRangeAxisProperty property changed handler.
        /// </summary>
        /// <param name="d">LineAreaBaseSeries that changed its DependentRangeAxis.</param>
        /// <param name="e">Event arguments.</param>
        private static void OnDependentRangeAxisPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (FastAnnotatedScatterSeries<TIndependent, TDependent>)d;
            IRangeAxis newValue = (IRangeAxis)e.NewValue;
            source.OnDependentRangeAxisPropertyChanged(newValue);
        }

        /// <summary>
        /// IndependentAxisProperty property changed handler.
        /// </summary>
        /// <param name="d">LineAreaBaseSeries that changed its IndependentAxis.</param>
        /// <param name="e">Event arguments.</param>
        private static void OnIndependentAxisPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (FastAnnotatedScatterSeries<TIndependent, TDependent>)d;
            IAxis newValue = (IAxis)e.NewValue;
            source.OnIndependentAxisPropertyChanged(newValue);
        }

        /// <summary>
        /// Adds the markers.
        /// </summary>
        /// <param name="pts">The points.</param>
        private void AddMarkers(IList<Point> pts)
        {
            var grp = new GeometryGroup { FillRule = FillRule.Nonzero };
            double sz = this.MarkerSize;
            double sz2 = sz / 2;

            switch (this.LineMarker)
            {
                case MarkerType.Square:
                    foreach (var pt in pts)
                    {
                        grp.Children.Add(new RectangleGeometry(new Rect(pt.X - sz2, pt.Y - sz2, sz, sz)));
                    }

                    break;
                case MarkerType.Circle:
                    foreach (var pt in pts)
                    {
                        grp.Children.Add(new EllipseGeometry(new Rect(pt.X - sz2, pt.Y - sz2, sz, sz)));
                    }

                    break;
                case MarkerType.Diamond:
                    foreach (var pt in pts)
                    {
                        grp.Children.Add(new RectangleGeometry(new Rect(pt.X - sz2, pt.Y - sz2, sz, sz)) { Transform = new RotateTransform(45, pt.X, pt.Y) });
                    }

                    break;
            }

            grp.Freeze();
            var path = new Path { Data = grp, Fill = this.Background };
            this.Canvas.Children.Add(path);

            for (int i = 0; i < pts.Count; i++)
            {
                var pt = pts[i];
                var tb = new TextBox
                             {
                                 Text = string.Format(this.TextFormat, this.points[i].Y), 
                                 FontSize = this.FontSize,
                                 Foreground = Brushes.Black,
                                 BorderBrush = this.AnnotationBrush
                             };

                Canvas.SetLeft(tb, pt.X + this.OffsetX);
                Canvas.SetTop(tb, pt.Y + this.OffsetY);
                this.Canvas.Children.Add(tb);
                this.Canvas.Children.Add(
                    new Line
                        {
                            Stroke = this.AnnotationBrush,
                            StrokeThickness = 1,
                            X1 = pt.X,
                            Y1 = pt.Y,
                            X2 = pt.X + this.OffsetX,
                            Y2 = pt.Y + this.OffsetY
                        });
            }
        }

        /// <summary>
        /// DependentRangeAxisProperty property changed handler.
        /// </summary>
        /// <param name="newValue">New value.</param>
        private void OnDependentRangeAxisPropertyChanged(IRangeAxis newValue)
        {
            this.InternalDependentAxis = newValue;
        }
        
        /// <summary>
        /// IndependentAxisProperty property changed handler.
        /// </summary>
        /// <param name="newValue">New value.</param>
        private void OnIndependentAxisPropertyChanged(IAxis newValue)
        {
            this.InternalIndependentAxis = newValue;
        }

        /// <summary>
        /// Gets the point collections.
        /// </summary>
        /// <typeparam name="T">The type of the collection.</typeparam>
        /// <param name="maximum">The maximum.</param>
        /// <param name="pts">The PTS.</param>
        /// <param name="func">The y function.</param>
        /// <returns>
        /// The point collection.
        /// </returns>
        private List<PointCollection> GetPointCollections<T>(double maximum, IList<T> pts, Func<T, TDependent> func)
            where T : ChartPoint<TIndependent, TDependent>
        {
            var l = new List<PointCollection>();
            PointCollection pc = new PointCollection();
            foreach (var dataPoint in this.points.Cast<T>())
            {
                if (dataPoint.IsMissing)
                {
                    if (pc.Count > 0)
                    {
                        l.Add(pc);
                        pc = new PointCollection();
                    }

                    continue;
                }

                var pt = new Point(
                    this.ActualIndependentAxis.GetPlotAreaCoordinate(dataPoint.X).Value,
                    maximum - this.ActualDependentRangeAxis.GetPlotAreaCoordinate(func(dataPoint)).Value);
                pc.Add(pt);
            }
            
            if (pc.Count > 0)
            {
                l.Add(pc);
            }

            return l;
        }
    }
}
