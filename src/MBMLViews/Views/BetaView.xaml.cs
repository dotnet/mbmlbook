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

    using Microsoft.Research.Glo;
    using Microsoft.Research.Glo.Views;

    using Microsoft.ML.Probabilistic.Distributions;

    /// <summary>
    /// Interaction logic for BetaView
    /// </summary>
    [ViewInformation(TargetType = typeof(Beta), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(IEnumerable<Beta>), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(IEnumerable<KeyValuePair<string, Beta>>), Priority = 12, MinimumSize = ViewSize.Cell)]
    public partial class BetaView : IConstrainableView, INotifyPropertyChanged
    {
        /// <summary>
        /// The betas.
        /// </summary>
        private readonly Dictionary<string, Beta> betas = new Dictionary<string, Beta>();
        
        /// <summary>
        /// The series.
        /// </summary>
        private Dictionary<string, Point[]> series;

        /// <summary>
        /// logarithmic y axis.
        /// </summary>
        private bool logarithmicYAxis;

        /// <summary>
        /// The title.
        /// </summary>
        private string title = "Beta Distribution Probability Density Function (PDF)";

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
        /// show the legend.
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
        /// Initializes a new instance of the <see cref="BetaView"/> class.
        /// </summary>
        public BetaView()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            InitializeComponent();
        }

        #region IConstrainableView Members
        /// <summary>
        /// The property changed.
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
                    CellText.Visibility = Visibility.Visible;
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
        [DisplayName("Logarithmic y-Axis")]
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
        [DisplayName("Chart title")]
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
        [DisplayName("X Minimum")]
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
        [DisplayName("X Maximum")]
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
        [DisplayName("Y Minimum")]
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
        [DisplayName("Y Maximum")]
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
        /// Gets or sets whether to show the legend.
        /// </summary>
        [DisplayName("Show Legend")]
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
        [DisplayName("Legend Position")]
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
        [DisplayName("Zoom")]
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
        [DisplayName("Legend Rows")]
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
        [DisplayName("Legend Columns")]
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
            this.betas.Clear();

            // Use slightly shortened version of string (without mean) for legend
            Func<Beta, string> keyFunc = beta => beta.ToString().Split('[')[0];

            Beta? b = DataContext as Beta?;
            if (b.HasValue)
            {
                this.betas.Add(keyFunc(b.Value), b.Value);
            }
            else
            {
                var betaEnumerable = DataContext as IEnumerable<Beta>;
                if (betaEnumerable != null)
                {
                    foreach (var beta in betaEnumerable.Where(beta => !this.betas.ContainsKey(keyFunc(beta))))
                    {
                        this.betas.Add(keyFunc(beta), beta);
                    }
                }
                else
                {
                    var betaDict = DataContext as IEnumerable<KeyValuePair<string, Beta>>;
                    if (betaDict == null)
                    {
                        return;
                    }

                    foreach (var kvp in betaDict.Where(kvp => !this.betas.ContainsKey(kvp.Key)))
                    {
                        this.betas.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            if (this.betas.Count == 0)
            {
                return;
            }

            CellText.Text = this.betas.First().Value.GetMean().ToString("#0.000");

            int interpolants = this.betas.Count > 10 ? 200 : 1000;
            
            this.series = this.betas.ToDictionary(ia => ia.Key, ia => this.GetData(ia.Value, interpolants).ToArray());

            MyChart.MaxNumberOfDataPoints = interpolants;
            
            MyChart.DataContext = this.Series;
            MyCellChart.DataContext = this.Series;
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <param name="beta">The beta.</param>
        /// <param name="count">The number of values.</param>
        /// <returns>The points.</returns>
        private IEnumerable<Point> GetData(Beta beta, int count)
        {
            IEnumerable<double> xs;
            if (this.LogarithmicYAxis)
            {
                xs = Enumerable.Range(1, count).Select(x => (double)x / (count + 1));
                return xs.Select(x => new Point(x, beta.GetLogProb(x)));
            }

            xs = Enumerable.Range(1, count).Select(x => (double)x / (count + 1));
            return xs.Select(x => new Point(x, Math.Exp(beta.GetLogProb(x))));
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
