// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLViews.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.DataVisualization.Charting;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Shapes;

    using Microsoft.Research.Glo;
    using Microsoft.Research.Glo.Views;

    using Microsoft.ML.Probabilistic;
    using Microsoft.ML.Probabilistic.Collections;
    using Microsoft.ML.Probabilistic.Distributions;
   
    /// <summary>
    /// Interaction logic for GaussianView
    /// </summary>
    [ViewInformation(TargetType = typeof(Gaussian), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(IEnumerable<Gaussian>), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(IEnumerable<KeyValuePair<string, Gaussian>>), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(Dictionary<string, Dictionary<MBMLCommon.DistributionType, Gaussian>>), Priority = 12, MinimumSize = ViewSize.SmallPanel)]
    public partial class GaussianView : IConstrainableView, INotifyPropertyChanged
    {
        /// <summary>
        /// The gaussians.
        /// </summary>
        private readonly List<KeyValuePair<string, Gaussian>> gaussians = new List<KeyValuePair<string, Gaussian>>();

        /// <summary>
        /// The series.
        /// </summary>
        private Dictionary<string, Point[]> series;

        /// <summary>
        /// The logarithmic y axis.
        /// </summary>
        private bool logarithmicYAxis;

        /// <summary>
        /// The title.
        /// </summary>
        private string title; // = "Gaussian Distribution Probability Density Function (PDF)";

        /// <summary>
        /// The x min.
        /// </summary>
        private double xMin = double.NaN;

        /// <summary>
        /// The x max.
        /// </summary>
        private double xMax = double.NaN;

        /// <summary>
        /// The y min.
        /// </summary>
        private double yMin = double.NaN;

        /// <summary>
        /// The y max.
        /// </summary>
        private double yMax = double.NaN;

        /// <summary>
        /// The show legend.
        /// </summary>
        private BooleanWithAuto showLegend = BooleanWithAuto.Auto;

        /// <summary>
        /// The legend position.
        /// </summary>
        private Dock legendPosition = Dock.Right;

        /// <summary>
        /// The zoom.
        /// </summary>
        private double zoom = 1.0;

        /// <summary>
        /// The view constraints.
        /// </summary>
        private ViewInformation viewConstraints;

        /// <summary>
        /// Draw the mean.
        /// </summary>
        private bool drawMean;

        /// <summary>
        /// Draw the standard deviation
        /// </summary>
        private bool drawStandardDeviation;

        /// <summary>
        /// The legend rows.
        /// </summary>
        private int legendRows;

        /// <summary>
        /// The legend columns.
        /// </summary>
        private int legendColumns = 1;

        /// <summary>
        /// The max to show.
        /// </summary>
        private int maxToShow = 10;

        /// <summary>
        /// The numeric axis string format.
        /// </summary>
        private string numericAxisStringFormat = "{0}";

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
        /// The x axis label.
        /// </summary>
        private string xAxisLabel = "x";

        /// <summary>
        /// The y axis label.
        /// </summary>
        private string yAxisLabel = "p(x)";

        /// <summary>
        /// The show x gridlines.
        /// </summary>
        private bool showXGridlines;

        /// <summary>
        /// The show y gridlines.
        /// </summary>
        private bool showYGridlines;

        /// <summary>
        /// The mean annotation.
        /// </summary>
        private string meanAnnotation;

        /// <summary>
        /// Initializes a new instance of the <see cref="GaussianView"/> class.
        /// </summary>
        public GaussianView()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            InitializeComponent();
        }

        #region IConstrainableView Members
        /// <summary>
        /// The property changed event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the view constraints.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ViewInformation ViewConstraints
        {
            get
            {
                return this.viewConstraints;
            }

            set
            {
                this.viewConstraints = value;

                if (this.viewConstraints.MaximumSize == ViewSize.Cell)
                {
                    MyChart.Visibility = Visibility.Collapsed;
                    MyCellChart.Visibility = Visibility.Visible;
                    ////CellText.Visibility = Visibility.Visible;
                    CellLegend.Visibility = Visibility.Visible;
                }
                else
                {
                    MyChart.MaxNumberOfDataPoints = 1500;
                }
            }
        }
        #endregion
    
        #region Public Properties promoted to the top
        /// <summary>
        /// Gets or sets the series.
        /// </summary>
        public Dictionary<string, Point[]> Series
        {
            get
            {
                return this.series;
            }

            set
            {
                if (this.series == value)
                {
                    return;
                }

                this.series = value;

                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the x axis label.
        /// </summary>
        [DisplayName(@"X Axis Label")]
        public string XAxisLabel
        {
            get
            {
                return this.xAxisLabel;
            }

            set
            {
                this.xAxisLabel = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the y axis label.
        /// </summary>
        [DisplayName(@"Y Axis Label")]
        public string YAxisLabel
        {
            get
            {
                return this.yAxisLabel;
            }

            set
            {
                this.yAxisLabel = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether logarithmic y axis.
        /// </summary>
        [DisplayName(@"Logarithmic y Axis")]
        public bool LogarithmicYAxis
        {
            get
            {
                return this.logarithmicYAxis;
            }

            set
            {
                if (this.logarithmicYAxis == value)
                {
                    return;
                }

                this.logarithmicYAxis = value;

                this.yMin = value ? double.NaN : 0.0;

                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [DisplayName(@"Title")]
        public string Title
        {
            get
            {
                return this.title;
            }

            set
            {
                if (this.title == value)
                {
                    return;
                }

                this.title = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the x minimum.
        /// </summary>
        [DisplayName(@"X Minimum")]
        public double XMinimum
        {
            get
            {
                return this.xMin;
            }

            set
            {
                if (Math.Abs(this.xMin - value) < double.Epsilon)
                {
                    return;
                }

                this.xMin = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the x maximum.
        /// </summary>
        [DisplayName(@"X Maximum")]
        public double XMaximum
        {
            get
            {
                return this.xMax;
            }

            set
            {
                if (Math.Abs(this.xMax - value) < double.Epsilon)
                {
                    return;
                }

                this.xMax = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the y minimum.
        /// </summary>
        [DisplayName(@"Y Minimum")]
        public double YMinimum
        {
            get
            {
                return this.yMin;
            }

            set
            {
                if (Math.Abs(this.yMin - value) < double.Epsilon)
                {
                    return;
                }

                this.yMin = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the y maximum.
        /// </summary>
        [DisplayName(@"Y Maximum")]
        public double YMaximum
        {
            get
            {
                return this.yMax;
            }
            
            set
            {
                if (Math.Abs(this.yMax - value) < double.Epsilon)
                {
                    return;
                }

                this.yMax = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the numeric axis string format.
        /// </summary>
        [DisplayName(@"Numeric Axis String Format")]
        public string NumericAxisStringFormat
        {
            get
            {
                return this.numericAxisStringFormat;
            }

            set
            {
                if (!value.StartsWith("{"))
                {
                    value = !value.StartsWith("0:") ? "{0:" + value : "{" + value;
                }

                if (!value.EndsWith("}"))
                {
                    value += "}";
                }

                this.numericAxisStringFormat = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the show legend.
        /// </summary>
        [DisplayName(@"Show Legend")]
        public BooleanWithAuto ShowLegend
        {
            get
            {
                return this.showLegend;
            }

            set
            {
                this.showLegend = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the legend position.
        /// </summary>
        [DisplayName(@"Legend Position")]
        public Dock LegendPosition
        {
            get
            {
                return this.legendPosition;
            }

            set
            {
                this.legendPosition = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the zoom.
        /// </summary>
        [DisplayName(@"Zoom")]
        public double Zoom
        {
            get
            {
                return this.zoom;
            }

            set
            {
                this.zoom = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to draw the mean.
        /// </summary>
        [DisplayName(@"Draw Mean")]
        public bool DrawMean
        {
            get
            {
                return this.drawMean;
            }

            set
            {
                this.drawMean = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the mean annotation.
        /// </summary>
        [DisplayName(@"Mean Annotation")]
        public string MeanAnnotation
        {
            get
            {
                return this.meanAnnotation;
            }

            set
            {
                this.meanAnnotation = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to draw the standard deviations.
        /// </summary>
        /// <value>
        /// <c>true</c> if [draw standard deviation]; otherwise, <c>false</c>.
        /// </value>
        [DisplayName(@"Draw Standard Deviation")]
        public bool DrawStandardDeviation
        {
            get
            {
                return this.drawStandardDeviation;
            }

            set
            {
                this.drawStandardDeviation = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the legend rows.
        /// </summary>
        [DisplayName(@"Legend Rows")]
        public int LegendRows
        {
            get
            {
                return this.legendRows;
            }

            set
            {
                this.legendRows = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the legend columns.
        /// </summary>
        [DisplayName(@"Legend Columns")]
        public int LegendColumns
        {
            get
            {
                return this.legendColumns;
            }

            set
            {
                this.legendColumns = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the max to show.
        /// </summary>
        [DisplayName(@"Max to Show")]
        public int MaxToShow
        {
            get
            {
                return this.maxToShow;
            }

            set
            {
                this.maxToShow = value;
                this.NotifyPropertyChanged();
                this.BuildView();
            }
        }

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
        /// Gets or sets a value indicating whether show x gridlines.
        /// </summary>
        [DisplayName(@"Show X Axis Gridlines")]
        public bool ShowXGridlines
        {
            get
            {
                return this.showXGridlines;
            }

            set
            {
                this.showXGridlines = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether show y gridlines.
        /// </summary>
        [DisplayName(@"Show Y Axis Gridlines")]
        public bool ShowYGridlines
        {
            get
            {
                return this.showYGridlines;
            }

            set
            {
                this.showYGridlines = value;
                this.NotifyPropertyChanged();
            }
        }

        #endregion

        /// <summary>
        /// Gets the palette.
        /// </summary>
        /// <value>
        /// The palette.
        /// </value>
        /// <exception cref="System.NullReferenceException">
        /// Style is null
        /// or
        /// Setter is null
        /// or
        /// Brush is null
        /// </exception>
        /// <exception cref="System.InvalidOperationException">Expected background property in setter</exception>
        public List<Brush> Palette
        {
            get
            {
                List<Brush> brushes = new List<Brush>();

                foreach (Style style in this.MyCellChart.Palette.Select(rd => rd["DataPointStyle"] as Style))
                {
                    if (style == null)
                    {
                        throw new NullReferenceException("Style is null");
                    }

                    Setter setter = style.Setters[0] as Setter;
                    if (setter == null)
                    {
                        throw new NullReferenceException("Setter is null");
                    }

                    if (setter.Property.ToString() != "Background")
                    {
                        throw new InvalidOperationException("Expected background property in setter");
                    }

                    Brush brush = setter.Value as Brush;

                    if (brush == null)
                    {
                        throw new NullReferenceException("Brush is null");
                    }

                    brushes.Add(brush);
                }

                return brushes;
            }
        }

        /// <summary>
        /// Formats the text.
        /// </summary>
        /// <param name="gaussian">The gaussian.</param>
        /// <returns>the text.</returns>
        private static string FormatText(Gaussian gaussian)
        {
            return string.Format("Gaussian({0}, {1})", gaussian.GetMean().ToString("#0.0"), gaussian.GetVariance().ToString("#0.0"));
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
        /// Builds the view.
        /// </summary>
        private void BuildView()
        {
            bool usePriorPosteriors = false;
            var priorPosteriorIndices = new List<Pair<int, int>>();
            
            this.gaussians.Clear();
            
            Gaussian? g = DataContext as Gaussian?;
            if (g.HasValue)
            {
                this.gaussians.Add(new KeyValuePair<string, Gaussian>(g.Value.ToString(), g.Value));
            }
            else
            {
                var gaussianEnumerable = DataContext as IEnumerable<Gaussian>;
                if (gaussianEnumerable != null)
                {
                    foreach (
                        var gaussian in
                            gaussianEnumerable.Where(gaussian => this.gaussians.All(ia => ia.Key != gaussian.ToString())).Take(this.MaxToShow))
                    {
                        this.gaussians.Add(new KeyValuePair<string, Gaussian>(gaussian.ToString(), gaussian));
                    }
                }
                else
                {
                    var gaussianDict = DataContext as IEnumerable<KeyValuePair<string, Gaussian>>;
                    if (gaussianDict != null)
                    {
                        foreach (var kvp in gaussianDict.Where(kvp => this.gaussians.All(ia => ia.Key != kvp.Key)).Take(this.MaxToShow))
                        {
                            this.gaussians.Add(new KeyValuePair<string, Gaussian>(kvp.Key, kvp.Value));
                        }
                    }
                    else
                    {
                        var dictOfDicts = DataContext as Dictionary<string, Dictionary<MBMLCommon.DistributionType, Gaussian>>;
                        int i = 0;
                        if (dictOfDicts != null)
                        {
                            foreach (var dict in dictOfDicts)
                            {
                                priorPosteriorIndices.Add(new Pair<int, int>(i, i + 1));
                                i += 2;

                                foreach (var kvp in dict.Value)
                                {
                                    this.gaussians.Add(
                                        new KeyValuePair<string, Gaussian>(dict.Key + string.Format(" ({0})", kvp.Key), kvp.Value));
                                }
                            }

                            usePriorPosteriors = true;
                        }
                    }
                }
            }

            if (this.gaussians.Count == 0)
            {
                return;
            }

            double min = double.PositiveInfinity;
            double max = double.NegativeInfinity;

            // loop over gaussians to get x range, using 3x sigma on each side
            foreach (var kvp in this.gaussians)
            {
                min = Math.Min(min, -(4 * Math.Sqrt(kvp.Value.GetVariance())) + kvp.Value.GetMean());
                max = Math.Max(max, (4 * Math.Sqrt(kvp.Value.GetVariance())) + kvp.Value.GetMean());
            }

            List<Brush> palette = this.Palette;

            foreach (var pair in this.gaussians.Select((kvp, i) => new { kvp.Value, i }))
            {
                this.CellLegend.Children.Add(
                    new BulletDecorator
                        {
                            Bullet = new Rectangle
                                         {
                                             Fill = palette[pair.i % palette.Count],
                                             Width = 8,
                                             Height = 8
                                         },
                            VerticalAlignment = VerticalAlignment.Center,
                            Child =
                                new TextBlock
                                    {
                                        Text = FormatText(pair.Value),
                                        FontSize = 12,
                                        Margin = new Thickness(3, 0, 0, 0)
                                    }
                        });
            }

            ////CellText.Text = string.Join(
            ////    "\n", this.gaussians.Select(ia => string.Format("Gaussian({0}, {1})", ia.GetMean().ToString("#0.0"), ia.GetVariance().ToString("#0.0"))));

            int interpolants = this.gaussians.Count > 10 ? 200 : 1000;

            var range = new RealRange { Min = min, Max = max, Steps = interpolants };

            // Use slightly shortened version of string (without mean) for legend
            this.series = this.gaussians.ToDictionary(ia => ia.Key, ia => this.GetData(ia.Value, range).ToArray());
            
            // Enumerate over gaussians again to add means after all Gaussians
            foreach (var kvp in this.gaussians)
            {
                if (this.LogarithmicYAxis || !this.DrawMean)
                {
                    continue;
                }

                double mean = kvp.Value.GetMean();
                string mkey = string.Format("μ={0}", mean.ToString("N"));

                if (!this.series.ContainsKey(mkey))
                {
                    this.series.Add(mkey, this.GetMeanSeries(kvp.Value));
                }
            }

            // And again for standard deviations
            foreach (var kvp in this.gaussians)
            {
                if (this.LogarithmicYAxis || !this.DrawStandardDeviation)
                {
                    continue;
                }

                double stdDev = Math.Sqrt(kvp.Value.GetVariance());
                string mkey = string.Format("σ={0}", stdDev.ToString("N"));

                if (this.series.ContainsKey("±" + mkey))
                {
                    continue;
                }

                this.series.Add("±" + mkey, this.GetStandardDeviationSeries(kvp.Value));

                this.yMin = 0.0;
                this.PropertyChanged(this, new PropertyChangedEventArgs("YMinimum"));
            }

            MyChart.MaxNumberOfDataPoints = interpolants;
            
            MyChart.DataContext = this.Series;
            MyCellChart.DataContext = this.Series;

            if (this.DrawMean && !string.IsNullOrEmpty(this.MeanAnnotation))
            {
                double mean = this.gaussians[0].Value.GetMean();
                double yval = Math.Exp(this.gaussians[0].Value.GetLogProb(mean)) / 4;

                // Add extra fake series
                this.MyChart.Series.Add(
                    new FastAnnotatedScatterSeries<double, double>
                        {
                            ItemsSource =
                                new[] { new ChartPoint<double, double> { X = mean, Y = yval } },
                            LineMarker = MarkerType.None,
                            MarkerSize = 0,
                            OffsetX = 10,
                            OffsetY = 2,
                            TextFormat = this.MeanAnnotation,
                            AnnotationBrush = Brushes.Black,
                            FontSize = 16,
                            LegendItemStyle = (Style)this.Resources["NullLegendItem"]
                        });

                // None of the below works to actually remove the legend item ...
                ////MyChart.LegendItems.RemoveAt(MyChart.LegendItems.Count - 1);
                ////var legendItemsList = MyChart.LegendItems as AggregatedObservableCollection<object>;
                ////if (legendItemsList != null)
                ////{
                ////    legendItemsList.ChildCollections.Remove(this.MyChart.Series.Last().LegendItems);
                ////}
            }

            if (!usePriorPosteriors)
            {
                return;
            }

            this.MyChart.Series.Clear();
            var p = this.MyChart.Palette;
            this.MyChart.Palette = null;

            var newPalette = new Collection<ResourceDictionary>();

            int idx = 0;
            foreach (var kvp in this.series)
            {
                // Assume priors come before posteriors
                bool isPrior = (idx++ % 2) == 0;
                newPalette.Add(isPrior ? p[idx / 2] : newPalette.Last());

                var fs = (FastSeries)this.CreateFastSeries(kvp.Value.Select(ia => new ChartPoint<double, double>(ia.X, ia.Y)), isPrior);
                fs.Title = kvp.Key;

                this.MyChart.Series.Add(fs);
            }

            this.MyChart.Palette = newPalette;
        }

        /// <summary>
        /// Gets the mean series.
        /// </summary>
        /// <param name="gaussian">The gaussian.</param>
        /// <returns>The <see cref="Point"/> array.</returns>
        private Point[] GetMeanSeries(Gaussian gaussian)
        {
            double mean = gaussian.GetMean();
            return new[]
                       {
                           new Point(mean - double.Epsilon, 0),
                           this.GetData(gaussian, new RealRange { Min = mean, Max = mean, Steps = 2 }).First(),
                           new Point(mean + double.Epsilon, 0)
                       };
        }

        /// <summary>
        /// Gets the standard deviation series.
        /// </summary>
        /// <param name="gaussian">The gaussian.</param>
        /// <returns>The <see cref="Point"/> array.</returns>
        private Point[] GetStandardDeviationSeries(Gaussian gaussian)
        {
            double mean = gaussian.GetMean();
            double stdDev = Math.Sqrt(gaussian.GetVariance());
            return new[]
                       {
                           new Point(mean - stdDev - double.Epsilon, 0),
                           this.GetData(gaussian, new RealRange { Min = mean - stdDev, Max = mean - stdDev, Steps = 2 }).First(),
                           new Point(mean - stdDev + double.Epsilon, 0),
                           new Point(mean - stdDev + double.Epsilon, -Math.Exp(gaussian.GetLogProb(mean))),
                           new Point(mean + stdDev - double.Epsilon, -Math.Exp(gaussian.GetLogProb(mean))),
                           new Point(mean + stdDev - double.Epsilon, 0),
                           this.GetData(gaussian, new RealRange { Min = mean + stdDev, Max = mean + stdDev, Steps = 2 }).First(),
                           new Point(mean + stdDev + double.Epsilon, 0)
                       };
        }

        /// <summary>
        /// Creates the fast series.
        /// </summary>
        /// <typeparam name="TIndependent">The type of the independent.</typeparam>
        /// <typeparam name="TDependent">The type of the dependent.</typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="dashed">if set to <c>true</c> [dashed].</param>
        /// <returns>
        /// The fast series.
        /// </returns>
        private ISeries CreateFastSeries<TIndependent, TDependent>(IEnumerable<ChartPoint<TIndependent, TDependent>> enumerable, bool dashed = false)
            where TIndependent : IComparable
            where TDependent : IComparable
        {
            return dashed
                       ? new FastDashedSeries<TIndependent, TDependent>
                                      {
                                          // LegendItemStyle = (Style)this.Resources["NullLegendItem"],
                                          LegendItemStyle = (Style)this.MyChart.Resources["CustomLegendItem"],
                                          ItemsSource = enumerable
                                      }
                       : new FastLineSeries<TIndependent, TDependent>
                             {
                                 LegendItemStyle = (Style)this.MyChart.Resources["CustomLegendItem"],
                                 ItemsSource = enumerable
                             };
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <param name="gaussian">The gaussian.</param>
        /// <param name="range">The range.</param>
        /// <returns>
        /// The points.
        /// </returns>
        private IEnumerable<Point> GetData(Gaussian gaussian, RealRange range)
        {
            Func<double, double> f = x => this.LogarithmicYAxis ? gaussian.GetLogProb(x) : Math.Exp(gaussian.GetLogProb(x));
            return range.Values.Select(x => new Point(x, f(x)));
        }
        
        #region INotifyPropertyChanged Members
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

        #endregion

        /// <summary>
        /// The extended line series.
        /// </summary>
        public class ExtendedLineSeries : LineSeries
        {
            /// <summary>
            /// The current labels.
            /// </summary>
            private readonly Dictionary<DataPoint, TextBlock> currentLabels = new Dictionary<DataPoint, TextBlock>();
            
            /// <summary>
            /// The labels canvas.
            /// </summary>
            private Canvas labelsCanvas;
            
            /// <summary>
            /// Gets or sets a value indicating whether labels should be displayed. 
            /// </summary>
            public bool DisplayLabels { get; set; }

            /// <summary>
            /// Gets or sets the binding path of the label.
            /// </summary>
            public string LabelBindingPath { get; set; }

            /// <summary>
            /// Gets or sets the style of each label.
            /// </summary>
            public Style LabelStyle { get; set; }

            /// <summary>
            /// Called when [apply template].
            /// </summary>
            public override void OnApplyTemplate()
            {
                base.OnApplyTemplate();

                // get a canvas to which the labels will be added
                this.labelsCanvas = (Canvas)this.GetTemplateChild("PlotArea");
                
                // clear the clip property so that labels are visible even if they exceed the bounds of the chart
                this.Clip = null;
            }

            /// <summary>
            /// Updates the data point.
            /// </summary>
            /// <param name="dataPoint">The data point.</param>
            protected override void UpdateDataPoint(DataPoint dataPoint)
            {
                base.UpdateDataPoint(dataPoint);

                // after the data point is created and added to the chart, we can add a label near it
                if (this.DisplayLabels && dataPoint.Visibility == Visibility.Visible)
                {
                    //// Dispatcher.BeginInvoke(() => this.CreateLabel(dataPoint));
                    this.CreateLabel(dataPoint);
                }
            }

            /// <summary>
            /// Creates the label.
            /// </summary>
            /// <param name="dataPoint">The data point.</param>
            private void CreateLabel(DataPoint dataPoint)
            {
                // this method is also called with the SizeChanged event, so I create the label only one time
                TextBlock label;
                if (this.currentLabels.ContainsKey(dataPoint))
                {
                    label = this.currentLabels[dataPoint];
                }
                else
                {
                    label = new TextBlock();
                    this.labelsCanvas.Children.Add(label);
                    this.currentLabels.Add(dataPoint, label);

                    label.Style = this.LabelStyle;

                    // bind the label text to the specified path, or to dataPoint.DependantValue by default
                    Binding binding = this.LabelBindingPath != null
                                ? new Binding(this.LabelBindingPath) { Source = dataPoint.DataContext }
                                : new Binding("DependentValue") { Source = dataPoint };
                    BindingOperations.SetBinding(label, TextBlock.TextProperty, binding);
                }

                // calculate a position of the label
                double coordinateY = Canvas.GetTop(dataPoint) - label.ActualHeight; // position the label above the data point
                double coordinateX = Canvas.GetLeft(dataPoint) + (dataPoint.ActualHeight / 2) - (label.ActualWidth / 2); // center horizontally
                Canvas.SetTop(label, coordinateY);
                Canvas.SetLeft(label, coordinateX);
            }
        }
    }
}
