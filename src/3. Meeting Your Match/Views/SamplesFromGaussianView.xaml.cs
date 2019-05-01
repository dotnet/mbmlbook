// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Controls.DataVisualization.Charting;

    using Microsoft.Research.Glo;
    using Microsoft.Research.Glo.Views;

    using MBMLViews;

    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Math;

    using MeetingYourMatch.Annotations;

    /// <summary>
    /// Interaction logic for SamplesFromGaussianView.xaml
    /// </summary>
    [ViewInformation(TargetType = typeof(Gaussian), Priority = 10, MinimumSize = ViewSize.Cell)]
    public partial class SamplesFromGaussianView : INotifyPropertyChanged
    {
        /// <summary>
        /// The bins.
        /// </summary>
        private int bins = 32;

        /// <summary>
        /// The samples.
        /// </summary>
        private int samples = 100;

        /// <summary>
        /// The linear axis interval.
        /// </summary>
        private double? linearAxisInterval;

        /// <summary>
        /// The chart data.
        /// </summary>
        private object chartData;

        /// <summary>
        /// The x minimum.
        /// </summary>
        private double xMinimum = double.NaN;

        /// <summary>
        /// The x maximum.
        /// </summary>
        private double xMaximum = double.NaN;

        /// <summary>
        /// The y maximum.
        /// </summary>
        private double yMaximum = double.NaN;

        /// <summary>
        /// Initializes a new instance of the <see cref="SamplesFromGaussianView"/> class.
        /// </summary>
        public SamplesFromGaussianView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the bins.
        /// </summary>
        [DisplayName(@"Bins")]
        public int Bins
        {
            get
            {
                return this.bins;
            }

            set
            {
                this.bins = value;
                this.OnPropertyChanged();
                this.BuildView();
            }
        }

        /// <summary>
        /// Gets or sets the samples.
        /// </summary>
        [DisplayName(@"Samples")]
        public int Samples
        {
            get
            {
                return this.samples;
            }

            set
            {
                this.samples = value;
                this.OnPropertyChanged();
                this.BuildView();
            }
        }

        /// <summary>
        /// Gets or sets the linear axis interval.
        /// </summary>
        [DisplayName(@"Linear Axis Interval")]
        public double? LinearAxisInterval
        {
            get
            {
                return this.linearAxisInterval;
            }

            set
            {
                if (value.Equals(this.linearAxisInterval))
                {
                    return;
                }

                this.linearAxisInterval = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the gaussian.
        /// </summary>
        public Gaussian Gaussian { get; set; }

        /// <summary>
        /// Gets or sets the chart data.
        /// </summary>
        public object ChartData
        {
            get
            {
                return this.chartData;
            }
         
            set
            {
                if (ReferenceEquals(value, this.chartData))
                {
                    return;
                }
            
                this.chartData = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the x minimum.
        /// </summary>
        [DisplayName(@"x-axis minimum")]
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
                this.OnPropertyChanged();
                this.BuildView();
            }
        }

        /// <summary>
        /// Gets or sets the x maximum.
        /// </summary>
        [DisplayName(@"x-axis maximum")]
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
                this.OnPropertyChanged();
                this.BuildView();
            }
        }

        /// <summary>
        /// Gets or sets the y maximum.
        /// </summary>
        [DisplayName(@"y-axis maximum")]
        public double YMaximum
        {
            get
            {
                return this.yMaximum;
            }

            set
            {
                if (value.Equals(this.yMaximum))
                {
                    return;
                }

                this.yMaximum = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Called when the data context is changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void SamplesFromGaussianViewOnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var ng = DataContext as Gaussian?;
            if (!ng.HasValue)
            {
                return;
            }

            if (this.Gaussian == ng.Value)
            {
                return;
            }

            this.Gaussian = ng.Value;
            this.BuildView();
        }

        /// <summary>
        /// Builds the view.
        /// </summary>
        private void BuildView()
        {
            // Set random seed
            Rand.Restart(100);

            double mean = this.Gaussian.GetMean();
            double variance = this.Gaussian.GetVariance();

            // Use range rather than repeat because otherwise the same sample is copied
            double[] x = Enumerable.Range(0, this.Samples).Select(ia => this.Gaussian.Sample()).ToArray();

            double[] binBoundaries;
            int[] binPositions;
            double binWidth;

            double minMax = Math.Sqrt(variance) * 4.0;
            double xMin = double.IsNaN(this.XMinimum) ? mean - minMax : this.XMinimum;
            double xMax = double.IsNaN(this.XMaximum) ? mean + minMax : this.XMaximum;

            int[] binned = x.Bin(this.Bins, xMin, xMax, out binBoundaries, out binPositions, out binWidth);

            double[] binCentres = binBoundaries.Zip(binBoundaries.Skip(1), (ia, ib) => (ia + ib) / 2).ToArray();

            IEnumerable<string> labels =
                binBoundaries.Skip(1).Select((ia, i) => binBoundaries[i].ToString("N0") + "-" + ia.ToString("N0")).ToArray();

            var dict =
                labels.Select((ia, i) => new KeyValuePair<string, double>(ia, binned[i] / (double)this.Samples))
                    .ToDictionary(ia => ia.Key, ia => ia.Value);

            var pts = binCentres.Zip(binned, (bin, count) => new Point(bin, count)).ToArray();

            this.LinearAxisInterval = Math.Sqrt(this.Gaussian.GetVariance());
            this.ChartData = dict;

            // Fake data with same x-axis to fool WpfChartView into creating the axis
            var chartPoints = pts.Select(pt => new ChartPoint<double, double>(pt.X, 0));
            MyChart.Series.Add(new FastLineSeries<double, double> { ItemsSource = chartPoints, });
            MyChart.Palette = (Collection<ResourceDictionary>)this.Resources["Palette"];
        }

        /// <summary>
        /// Called when the view layout is updated.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void SamplesFromGaussianViewOnLayoutUpdated(object sender, EventArgs e)
        {
            if (MyChart.ActualAxes.Count == 3)
            {
                ((CategoryAxis)MyChart.ActualAxes[2]).Visibility = Visibility.Hidden;
            }
        }
    }
}
