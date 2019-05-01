// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Views
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows;

    using Microsoft.Research.Glo;
    using Microsoft.Research.Glo.Views;

    using MBMLViews.Views;

    /// <summary>
    /// Interaction logic for PerformanceSpaceView.xaml
    /// </summary>
    [ViewInformation(TargetType = typeof(PerformanceSpaceViewModel), Priority = 10, MinimumSize = ViewSize.LargePanel)]
    [Feature(Description = "Performance Space View", Date = "15/08/2013")]
    public partial class PerformanceSpace2View : IConstrainableView, INotifyPropertyChanged
    {
        /// <summary>
        /// The data point size.
        /// </summary>
        private double dataPointSize = 2.5;

        /// <summary>
        /// The show annotation borders.
        /// </summary>
        private bool showAnnotationBorders = true;

        /// <summary>
        /// The x minimum.
        /// </summary>
        private double xMinimum = double.NaN;

        /// <summary>
        /// The x maximum.
        /// </summary>
        private double xMaximum = double.NaN;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceSpace2View"/> class.
        /// </summary>
        public PerformanceSpace2View()
        {
            InitializeComponent();
            this.Width = 300;
            this.Height = 300;
        }

        /// <summary>
        /// The property changed event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the data point size.
        /// </summary>
        [DisplayName(@"DataPoint Size")]
        public double DataPointSize
        {
            get
            {
                return this.dataPointSize;
            }

            set
            {
                this.dataPointSize = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether show annotation borders.
        /// </summary>
        [DisplayName(@"Show Annotation Borders")]
        public bool ShowAnnotationBorders
        {
            get
            {
                return this.showAnnotationBorders;
            }

            set
            {
                this.showAnnotationBorders = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the x minimum.
        /// </summary>
        public double XMinimum
        {
            get
            {
                return this.xMinimum;
            }

            set
            {
                if (value.Equals(this.xMinimum))
                {
                    return;
                }

                this.xMinimum = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the x maximum.
        /// </summary>
        public double XMaximum
        {
            get
            {
                return this.xMaximum;
            }

            set
            {
                if (value.Equals(this.xMaximum))
                {
                    return;
                }

                this.xMaximum = value;
                this.NotifyPropertyChanged();
            }
        }

        #region IConstrainableView Members
        /// <summary>
        /// Gets or sets the view constraints.
        /// </summary>
        public ViewInformation ViewConstraints { get; set; }

        #endregion

        #region INotifyPropertyChanged Members
        /// <summary>
        /// Notifies the property changed.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        /// <summary>
        /// Handles the OnDataContextChanged event of the PerformanceSpace2View control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void PerformanceSpace2View_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var viewModel = DataContext as PerformanceSpaceViewModel;
            if (viewModel == null)
            {
                return;
            }

            const double StrokeThickness = 2.0;

            var diagonal = new[] { new Point(viewModel.XMinimum, viewModel.YMinimum), new Point(viewModel.XMaximum, viewModel.YMaximum) };
            MyChart.DataContext = diagonal;
            MyChart.MarkerSize = this.DataPointSize;
            
            var palette = this.Resources["Palette"] as Collection<ResourceDictionary>;
            if (palette == null)
            {
                return;
            }

            MyChart.Series.Clear();
            MyChart.Palette = null;
            
            var newPalette = new Collection<ResourceDictionary> { palette[0] };

            this.AddDiagonalLine(StrokeThickness, diagonal);
            this.AddDrawMargin(viewModel, StrokeThickness, palette, newPalette);
            this.AddSamples(viewModel, palette, newPalette);
            this.AddTextAnnotations(viewModel);

            MyChart.Palette = newPalette;
        }

        /// <summary>
        /// Adds the diagonal line.
        /// </summary>
        /// <param name="strokeThickness">The stroke thickness.</param>
        /// <param name="diagonal">The diagonal.</param>
        private void AddDiagonalLine(double strokeThickness, IEnumerable<Point> diagonal)
        {
            MyChart.Series.Add(
                new FastDashedSeries<double, double>
                {
                    ItemsSource =
                        diagonal.Select(
                            ia => new ChartPoint<double, double>(ia.X, ia.Y)),
                    StrokeDashArray = new[] { 1.0 },
                    StrokeThickness = strokeThickness,
                });
        }

        /// <summary>
        /// Adds the samples.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="palette">The palette.</param>
        /// <param name="newPalette">The new palette.</param>
        private void AddSamples(PerformanceSpaceViewModel viewModel, IReadOnlyList<ResourceDictionary> palette, ICollection<ResourceDictionary> newPalette)
        {
            if (viewModel.Samples == null || viewModel.Samples.Count() <= 0)
            {
                return;
            }

            MyChart.Series.Add(
                new FastScatterSeries<double, double>
                    {
                        ItemsSource =
                            viewModel.Samples.Select(ia => new ChartPoint<double, double>(ia.X, ia.Y)),
                    });
            newPalette.Add(palette[1]);
        }

        /// <summary>
        /// Adds the draw margin.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <param name="strokeThickness">The stroke thickness.</param>
        /// <param name="oldPalette">The old palette.</param>
        /// <param name="newPalette">The new palette.</param>
        private void AddDrawMargin(PerformanceSpaceViewModel viewModel, double strokeThickness, Collection<ResourceDictionary> oldPalette, Collection<ResourceDictionary> newPalette)
        {
            if (double.IsNaN(viewModel.DrawMargin) || !(viewModel.DrawMargin > 0))
            {
                return;
            }
            
            // Add draw margin lines
            var margin1 = new[]
                              {
                                  new ChartPoint<double, double>(viewModel.XMinimum, viewModel.YMinimum + viewModel.DrawMargin),
                                  new ChartPoint<double, double>(viewModel.XMaximum - viewModel.DrawMargin, viewModel.YMaximum)
                              };
            var margin2 = new[]
                              {
                                  new ChartPoint<double, double>(viewModel.XMinimum + viewModel.DrawMargin, viewModel.YMinimum),
                                  new ChartPoint<double, double>(viewModel.XMaximum, viewModel.YMaximum - viewModel.DrawMargin)
                              };
            MyChart.Series.Add(
                new FastDashedSeries<double, double>
                    {
                        StrokeDashArray = new[] { 0.5 },
                        StrokeThickness = strokeThickness,
                        ItemsSource = margin1
                    });
            MyChart.Series.Add(
                new FastDashedSeries<double, double>
                    {
                        StrokeDashArray = new[] { 0.5 },
                        StrokeThickness = strokeThickness,
                        ItemsSource = margin2
                    });
            newPalette.Add(oldPalette[0]);
            newPalette.Add(oldPalette[0]);

            // Now add the text
            var pt = new ChartPoint<double, double>(
                viewModel.XMinimum + ((viewModel.XMaximum - viewModel.XMinimum) * 0.84),
                viewModel.YMinimum + ((viewModel.YMaximum - viewModel.YMinimum) * 0.16));
            MyChart.Series.Add(new ChartAnnotation<double, double> { ItemsSource = new[] { pt }, Annotation = "Draw", ShowBorder = this.ShowAnnotationBorders });

            var mid = new ChartPoint<double, double>(
                viewModel.XMinimum + ((viewModel.XMaximum - viewModel.XMinimum) * 0.75),
                viewModel.YMinimum + ((viewModel.YMaximum - viewModel.YMinimum) * 0.75));
            var pt1 = new ChartPoint<double, double>(mid.X - viewModel.DrawMargin, mid.Y);
            var pt2 = new ChartPoint<double, double>(mid.X + viewModel.DrawMargin, mid.Y);
            var pt3 = new ChartPoint<double, double>(mid.X, mid.Y - viewModel.DrawMargin);
            var pt4 = new ChartPoint<double, double>(mid.X, mid.Y + viewModel.DrawMargin);
            
            MyChart.Series.Add(
                new FastArrowSeries<double, double> { ItemsSource = new[] { pt1, pt2, pt3, pt4 }, StrokeThickness = 1.5, IsStartArrow = true });
        }

        /// <summary>
        /// Adds the text annotations.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        private void AddTextAnnotations(PerformanceSpaceViewModel viewModel)
        {
            // Add the text annotations
            var pt1 = new ChartPoint<double, double>(
                viewModel.XMinimum + ((viewModel.XMaximum - viewModel.XMinimum) * 0.25),
                viewModel.YMinimum + ((viewModel.YMaximum - viewModel.YMinimum) * 0.25));
            var pt2 = new ChartPoint<double, double>(
                viewModel.XMinimum + ((viewModel.XMaximum - viewModel.XMinimum) * 0.75),
                viewModel.YMinimum + ((viewModel.YMaximum - viewModel.YMinimum) * 0.75));
            MyChart.Series.Add(new ChartAnnotation<double, double> { Annotation = viewModel.Player1Wins, ItemsSource = new[] { pt1 }, ShowBorder = this.ShowAnnotationBorders });
            MyChart.Series.Add(new ChartAnnotation<double, double> { Annotation = viewModel.Player2Wins, ItemsSource = new[] { pt2 }, ShowBorder = this.ShowAnnotationBorders });
        }
    }
}
