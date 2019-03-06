// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AssessingPeoplesSkills.Views
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.DataVisualization.Charting;
    using System.Windows.Media;

    using Microsoft.Research.Glo;
    using Microsoft.Research.Glo.Views;

    using MBMLViews;

    using Microsoft.ML.Probabilistic.Distributions;

    /// <summary>
    /// Custom view for AssessingPeoplesSkills project
    /// </summary>
    [ViewInformation(TargetType = typeof(Gaussian), Priority = 15, MinimumSize = ViewSize.LargePanel)]
    [Feature(Description = "Custom LearningSkills GaussianDemo view", Date = "11/03/2013")]
    public partial class GaussianDemoView : IConstrainableView, INotifyPropertyChanged
    {
        /// <summary>
        /// The threshold.
        /// </summary>
        private double threshold = 0.005;

        /// <summary>
        /// The gaussian.
        /// </summary>
        private Gaussian gaussian;

        /// <summary>
        /// The x maximum.
        /// </summary>
        private double xMaximum = double.NaN;

        /// <summary>
        /// The x minimum.
        /// </summary>
        private double xMinimum = double.NaN;

        /// <summary>
        /// The y minimum.
        /// </summary>
        private double yMaximum = double.NaN;

        /// <summary>
        /// The background red.
        /// </summary>
        private double backgroundR = double.NaN;

        /// <summary>
        /// The background green.
        /// </summary>
        private double backgroundG = double.NaN;

        /// <summary>
        /// The background blue.
        /// </summary>
        private double backgroundB = double.NaN;

        /// <summary>
        /// Initializes a new instance of the <see cref="GaussianDemoView"/> class. 
        /// </summary>
        public GaussianDemoView()
        {
            InitializeComponent();
            this.ViewConstraints = new ViewInformation { MinimumSize = ViewSize.SmallPanel };
            this.LineChart.Loaded += ChartLoaded;
            this.AreaChart.Loaded += ChartLoaded;
        }

        /// <summary>
        /// The property changed event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #region Properties promoted to the top

        /// <summary>
        /// Gets or sets the gaussian.
        /// </summary>
        public Gaussian Gaussian
        {
            get
            {
                return this.gaussian;
            }

            set
            {
                this.gaussian = value;

                double mean = this.gaussian.GetMean();
                double fourSigma = 4 * Math.Sqrt(this.gaussian.GetVariance());
                if (double.IsNaN(this.XMinimum))
                {
                    this.XMinimum = mean - fourSigma;
                }

                if (double.IsNaN(this.XMaximum))
                {
                    this.XMaximum = mean + fourSigma;
                }

                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged("LinePoints");
                this.NotifyPropertyChanged("AreaPoints");
            }
        }

        /// <summary>
        /// Gets the line points.
        /// </summary>
        public List<Point> LinePoints
        {
            get
            {
                var f1 = new Func<double, Point>(x => new Point(x, Math.Exp(this.Gaussian.GetLogProb(x))));
                return this.Range.Values.Select(f1).ToList();
            }
        }

        /// <summary>
        /// Gets the area points.
        /// </summary>
        public List<double> AreaPoints
        {
            get
            {
                var f1 = new Func<double, double>(x => Math.Exp(this.Gaussian.GetLogProb(x)));
                var f2 = new Func<double, double>(x => Math.Abs(x - this.Gaussian.GetMean()) > this.Threshold ? -1 : f1(x));
                return this.Range.Values.Select(f2).ToList();
            }
        }

        /// <summary>
        /// Gets the range.
        /// </summary>
        public RealRange Range
        {
            get
            {
                return new RealRange { Min = this.XMinimum, Max = this.XMaximum, Steps = 2000 };
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
                this.xMaximum = value;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged("Range");
                this.NotifyPropertyChanged("LinePoints");
                this.NotifyPropertyChanged("AreaPoints");
            }
        }

        /// <summary>
        /// Gets or sets the x maximum.
        /// </summary>
        public double XMinimum
        {
            get
            {
                return this.xMinimum;
            }

            set
            {
                this.xMinimum = value;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged("Range");
                this.NotifyPropertyChanged("LinePoints");
                this.NotifyPropertyChanged("AreaPoints");
            }
        }

        /// <summary>
        /// Gets or sets the y maximum.
        /// </summary>
        public double YMaximum
        {
            get
            {
                return this.yMaximum;
            }

            set
            {
                this.yMaximum = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the threshold.
        /// </summary>
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
        /// Gets or sets the background red value
        /// </summary>
        public double BackgroundR
        {
            get
            {
                return this.backgroundR;
            }

            set
            {
                this.backgroundR = value;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged("BackgroundColor");
            }
        }

        /// <summary>
        /// Gets or sets the background green value.
        /// </summary>
        public double BackgroundG
        {
            get
            {
                return this.backgroundG;
            }

            set
            {
                this.backgroundG = value;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged("BackgroundColor");
            }
        }

        /// <summary>
        /// Gets or sets the background blue value.
        /// </summary>
        public double BackgroundB
        {
            get
            {
                return this.backgroundB;
            }

            set
            {
                this.backgroundB = value;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged("BackgroundColor");
            }
        }


        /// <summary>
        /// Gets the background color.
        /// </summary>
        public Brush BackgroundColor
        {
            get
            {
                return !double.IsNaN(this.BackgroundR) && !double.IsNaN(this.BackgroundG) && !double.IsNaN(this.BackgroundB)
                           ? new SolidColorBrush(
                                 Color.FromRgb(
                                     (byte)(this.BackgroundR * 255),
                                     (byte)(this.BackgroundG * 255),
                                     (byte)(this.BackgroundB * 255)))
                           : Brushes.Transparent;
            }
        }

        #endregion

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

            // this.BuildView();
        }
        #endregion

        /// <summary>
        /// Finds the child.
        /// </summary>
        /// <typeparam name="T">The type of the child.</typeparam>
        /// <param name="parent">The parent.</param>
        /// <param name="childName">Name of the child.</param>
        /// <returns>The first matching child.</returns>
        private static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null)
            {
                return null;
            }

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                T childType = child as T;
                if (childType == null)
                {
                    T foundChild = FindChild<T>(child, childName);
                    if (foundChild != null)
                    {
                        return foundChild;
                    }
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    if (frameworkElement == null || frameworkElement.Name != childName)
                    {
                        continue;
                    }

                    return (T)child;
                }
                else
                {
                    return (T)child;
                }
            }

            return null;
        }

        /// <summary>
        /// The make axes transparent.
        /// </summary>
        /// <param name="chart">
        /// The chart.
        /// </param>
        private static void MakeAxesTransparent(DependencyObject chart)
        {
            foreach (var axis in new[] { "X", "Y" }.Select(name => FindChild<LinearAxis>(chart, name)).Where(axis => axis != null))
            {
                axis.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// Makes the chart background transparent.
        /// </summary>
        /// <param name="chart">The chart.</param>
        private static void MakeBackgroundTransparent(DependencyObject chart)
        {
            var border = FindChild<Border>(chart, "PlotAreaBorder");
            if (border != null)
            {
                border.BorderBrush = new SolidColorBrush(Colors.Transparent);
                border.Background = new SolidColorBrush(Colors.Transparent);
            }
        }

        /// <summary>
        /// Called when the chart is loaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private static void ChartLoaded(object sender, RoutedEventArgs e)
        {
            MakeBackgroundTransparent((FrameworkElement)sender);

            if (((WpfChartView)sender).Name == "AreaChart")
            {
                MakeAxesTransparent((FrameworkElement)sender);
            }
        }

        /// <summary>
        /// The user control data context changed.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void UserControlDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var g = DataContext as Gaussian?;
            if (!g.HasValue)
            {
                return;
            }

            this.Gaussian = g.Value;
        }
    }
}