// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLViews.Views
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media;
    using System.Windows.Shapes;

    using Microsoft.Research.Glo;
    using Microsoft.Research.Glo.Views;

    using Microsoft.ML.Probabilistic.Distributions;

    /// <summary>
    /// Interaction logic for GammaView
    /// </summary>
    [ViewInformation(TargetType = typeof(Gamma), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(IEnumerable<Gamma>), Priority = 12, MinimumSize = ViewSize.Cell)]
    public partial class GammaView : IConstrainableView, INotifyPropertyChanged
    {
        /// <summary>
        /// The gammas.
        /// </summary>
        private List<Gamma> gammas = new List<Gamma>();

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
        private string title = "Gamma Distribution Probability Density Function (PDF)";

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
        /// The legend rows.
        /// </summary>
        private int legendRows;

        /// <summary>
        /// The legend columns.
        /// </summary>
        private int legendColumns = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="GammaView"/> class.
        /// </summary>
        public GammaView()
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
                this.yMax = value;
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
        /// <param name="gamma">The gamma.</param>
        /// <returns>The text.</returns>
        private static string FormatText(Gamma gamma)
        {
            return string.Format("Gamma({0}, {1})", gamma.Shape.ToString("N"), gamma.GetScale().ToString("N"));
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
            this.gammas.Clear();

            Gamma? g = DataContext as Gamma?;
            if (g.HasValue)
            {
                this.gammas.Add(g.Value);
            }
            else
            {
                var gammaEnumerable = DataContext as IEnumerable<Gamma>;
                if (gammaEnumerable == null)
                {
                    return;
                }

                this.gammas = gammaEnumerable.ToList();
            }

            if (this.gammas.Count == 0)
            {
                return;
            }

            double min = double.PositiveInfinity;
            double max = double.NegativeInfinity;

            // loop over gammas to get x range, using 3x sigma on each side
            foreach (Gamma gamma in this.gammas)
            {
                min = double.Epsilon;
                max = Math.Max(max, (3 * Math.Sqrt(gamma.GetVariance())) + gamma.GetMean());
            }

            List<Brush> palette = this.Palette;

            for (int i = 0; i < this.gammas.Count; i++)
            {
                Gamma gamma = this.gammas[i];
                this.CellLegend.Children.Add(
                    new BulletDecorator
                        {
                            Bullet = new Rectangle
                            {
                                Fill = palette[i % palette.Count],
                                Width = 8,
                                Height = 8
                            },
                            VerticalAlignment = VerticalAlignment.Center,
                            Child =
                                new TextBlock
                                    {
                                        Text = FormatText(gamma),
                                        FontSize = 12,
                                        Margin = new Thickness(3, 0, 0, 0)
                                    }
                        });
            }

            ////CellText.Text = string.Join(
            ////    "\n", this.gammas.Select(ia => string.Format("Gamma({0}, {1})", ia.GetMean().ToString("#0.0"), ia.GetVariance().ToString("#0.0"))));

            int interpolants = this.gammas.Count > 10 ? 200 : 1000;

            // Use slightly shortened version of string (without mean) for legend
            this.series = new Dictionary<string, Point[]>();
            foreach (Gamma gamma in this.gammas)
            {
                string key = FormatText(gamma);
                if (!this.series.ContainsKey(key))
                {
                    this.series.Add(key, this.GetData(gamma, min, max, interpolants).ToArray());
                }
            }

            MyChart.MaxNumberOfDataPoints = interpolants;
            
            MyChart.DataContext = this.Series;
            MyCellChart.DataContext = this.Series;
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <param name="gamma">The gamma.</param>
        /// <param name="min">The x min.</param>
        /// <param name="max">The x max.</param>
        /// <param name="count">The number of points.</param>
        /// <returns>The points.</returns>
        private IEnumerable<Point> GetData(Gamma gamma, double min, double max, int count)
        {
            IEnumerable<double> xs;
            if (this.LogarithmicYAxis)
            {
                xs = Enumerable.Range(1, count).Select(x => min + ((max - min) * ((double)x / (count - 1))));
                return xs.Select(x => new Point(x, gamma.GetLogProb(x)));
            }

            xs = Enumerable.Range(0, count).Select(x => min + ((max - min) * ((double)x / (count - 1))));
            return xs.Select(x => new Point(x, Math.Exp(gamma.GetLogProb(x))));
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
    }
}
