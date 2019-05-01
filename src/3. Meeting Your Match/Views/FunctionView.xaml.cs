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
    using System.Windows.Controls.DataVisualization.Charting;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Shapes;

    using Microsoft.Research.Glo;
    using Microsoft.Research.Glo.Views;

    /// <summary>
    /// Interaction logic for GaussianView
    /// </summary>
    [ViewInformation(TargetType = typeof(FunctionViewModel), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(IEnumerable<FunctionViewModel>), Priority = 12, MinimumSize = ViewSize.Cell)]
    public partial class FunctionView : IConstrainableView, INotifyPropertyChanged
    {
        /// <summary>
        /// The functions.
        /// </summary>
        private List<FunctionViewModel> functions = new List<FunctionViewModel>();

        /// <summary>
        /// The series.
        /// </summary>
        private Dictionary<string, Point[]> series;

        /// <summary>
        /// Logarithmic y axis.
        /// </summary>
        private bool logarithmicYAxis;

        /// <summary>
        /// The title.
        /// </summary>
        private string title = "1D function plot";

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
        /// Whether to show the legend.
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
        /// Initializes a new instance of the <see cref="FunctionView"/> class.
        /// </summary>
        public FunctionView()
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
        [DisplayName("Logarithmic y Axis")]
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
        [DisplayName("Title")]
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
        [DisplayName("X Maximum")]
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
        [DisplayName("Y Minimum")]
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
        [DisplayName("Y Maximum")]
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
        /// Gets or sets the show legend.
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
            this.functions.Clear();

            var f = DataContext as FunctionViewModel;
            if (f != null)
            {
                this.functions.Add(f);
            }
            else
            {
                var funcEnumerable = DataContext as IEnumerable<FunctionViewModel>;
                if (funcEnumerable == null)
                {
                    return;
                }

                this.functions = funcEnumerable.ToList();
            }

            if (this.functions.Count == 0)
            {
                return;
            }

            double min = double.PositiveInfinity;
            double max = double.NegativeInfinity;

            // loop over gaussians to get x range, using 3x sigma on each side
            foreach (FunctionViewModel viewModel in this.functions)
            {
                min = Math.Min(min, viewModel.Range.Min);
                max = Math.Max(max, viewModel.Range.Max);
            }

            List<Brush> palette = this.Palette;

            for (int i = 0; i < this.functions.Count; i++)
            {
                var func = this.functions[i];
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
                                Text = func.Name,
                                FontSize = 12,
                                Margin = new Thickness(3, 0, 0, 0)
                            }
                    });
            }

            int interpolants = this.functions.Count > 10 ? 200 : 1000;

            // Use slightly shortened version of string (without mean) for legend
            this.series = new Dictionary<string, Point[]>();
            foreach (FunctionViewModel viewModel in this.functions)
            {
                string key = viewModel.Name;
                if (this.series.ContainsKey(key))
                {
                    continue;
                }

                this.series.Add(key, viewModel.Points);
            }

            MyChart.MaxNumberOfDataPoints = interpolants;

            MyChart.DataContext = this.Series;
            MyCellChart.DataContext = this.Series;
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
