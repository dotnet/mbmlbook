// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Views
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Media;

    using Microsoft.Research.Glo;
    using Microsoft.Research.Glo.Views;

    using MBMLViews;
    using MBMLViews.Views;

    using Microsoft.ML.Probabilistic.Distributions;

    using Point = System.Windows.Point;

    /// <summary>
    /// Interaction logic for GaussianCdfDemoView.xaml
    /// </summary>
    [ViewInformation(TargetType = typeof(Gaussian), Priority = 10, MinimumSize = ViewSize.Cell)]
    public partial class GaussianCdfDemoView : INotifyPropertyChanged
    {
        /// <summary>
        /// The show axis lines.
        /// </summary>
        private bool showAxisLines;

        /// <summary>
        /// The show x axis.
        /// </summary>
        private bool showXAxis = true;

        /// <summary>
        /// The show y axis.
        /// </summary>
        private bool showYAxis = true;

        /// <summary>
        /// The threshold.
        /// </summary>
        private double threshold = -1.0;

        /// <summary>
        /// Initializes a new instance of the <see cref="GaussianCdfDemoView"/> class.
        /// </summary>
        public GaussianCdfDemoView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The property changed event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets a value indicating whether show axis lines.
        /// </summary>
        [DisplayName(@"Show axis lines")]
        public bool ShowAxisLines
        {
            get
            {
                return this.showAxisLines;
            }

            set
            {
                this.showAxisLines = value;
                this.NotifyPropertyChanged();
                this.BuildView();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether show x axis.
        /// </summary>
        [DisplayName(@"Show x axis")]
        public bool ShowXAxis
        {
            get
            {
                return this.showXAxis;
            }

            set
            {
                this.showXAxis = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether show y axis.
        /// </summary>
        [DisplayName(@"Show y axis")]
        public bool ShowYAxis
        {
            get
            {
                return this.showYAxis;
            }

            set
            {
                this.showYAxis = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the threshold.
        /// </summary>
        [DisplayName(@"Threshold")]
        public double Threshold
        {
            get
            {
                return this.threshold;
            }

            set
            {
                this.threshold = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Builds the view.
        /// </summary>
        private void BuildView()
        {
            var gaussian = this.DataContext is Gaussian ? (Gaussian)this.DataContext : new Gaussian(0, 1);

            Func<Gaussian, double, double> thresholdFunc =
                (g, ia) => ia < this.Threshold ? Math.Exp(g.GetLogProb(ia)) : 0;

            Func<Gaussian, double, double> pdf = 
                (g, ia) => Math.Exp(g.GetLogProb(ia));

            const int Samples = 1500;

            var x = Enumerable.Range(0, Samples).Select(ia => (ia - (Samples / 2.0)) / Samples * 6.0).ToArray();
            var series = new[]
                             {
                                 x.Select(ia => new Point(ia, pdf(gaussian, ia))).ToArray(),
                                 x.Select(ia => new Point(ia, gaussian.CumulativeDistributionFunction(ia))).ToArray(),
                                 x.Select(ia => new Point(ia, thresholdFunc(gaussian, ia))).ToArray(),
                                 new[] { new Point(this.Threshold, gaussian.CumulativeDistributionFunction(this.Threshold)) } 
                             };

            MyChart.DataContext = series;
            MyChart.Series.Clear();

            var p = MyChart.Palette;
            MyChart.Palette = null;

            var newPalette = new Collection<ResourceDictionary> { p[0], p[1], p[0], p[1] };

            Func<Point, ChartPoint<double, double>> pointConverter = pt => new ChartPoint<double, double>(pt.X, pt.Y);

            foreach (var s in series.Take(2))
            {
                MyChart.Series.Add(
                    new FastLineSeries<double, double>
                        {
                            ItemsSource = s.Select(pointConverter).ToArray(),
                        });
            }

            MyChart.Series.Add(new FastAreaSeries<double, double>
                                        {
                                            ItemsSource = series[2].Select(pointConverter).ToArray(),
                                            Opacity = 0.2
                                        });

            MyChart.Series.Add(new FastAnnotatedScatterSeries<double, double>
                                   {
                                       ItemsSource = series[3].Select(pointConverter).ToArray(),
                                       LineMarker = MarkerType.Diamond,
                                       MarkerSize = 5,
                                       OffsetX = 10,
                                       OffsetY = 2,
                                       TextFormat = "{0:N2}",
                                       AnnotationBrush = Brushes.Black,
                                       FontSize = 10
                                   });

            this.MyChart.Palette = newPalette;
        }

        /// <summary>
        /// Handles the DataContextChanged event of the UserControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void UserControlDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.BuildView();
        }

        /// <summary>
        /// The notify property changed.
        /// </summary>
        /// <param name="propertyName">
        /// The property name.
        /// </param>
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }

            this.BuildView();
        }
    }
}
