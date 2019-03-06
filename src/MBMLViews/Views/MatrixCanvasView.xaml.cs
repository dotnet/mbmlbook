// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// #define TESTING

namespace MBMLViews.Views
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;

    using Microsoft.Research.Glo;
    using Microsoft.Research.Glo.Object;

    using Microsoft.ML.Probabilistic.Math;

    using Matrix = Microsoft.ML.Probabilistic.Math.Matrix;
    using Vector = Microsoft.ML.Probabilistic.Math.Vector;
    
    /// <summary>
    /// Interaction logic for MatrixCanvasView
    /// </summary>
    [ViewInformation(TargetType = typeof(bool[][]), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(int[][]), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(double[][]), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(bool?[][]), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(int?[][]), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(double?[][]), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(IEnumerable<double[]>), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(bool[,]), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(int[,]), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(double[,]), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(bool?[,]), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(int?[,]), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(double?[,]), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(MaskedMatrix), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(Matrix), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(IList<Vector>), Priority = 12, MinimumSize = ViewSize.Cell)]
    [ViewInformation(TargetType = typeof(IList<SparseVector>), Priority = 12, MinimumSize = ViewSize.Cell)]
    public partial class MatrixCanvasView : IConstrainableView, INotifyPropertyChanged, IDataExpressionContext
    {
        #region DependencyProperties
        /// <summary>
        /// The color map property.
        /// </summary>
        public static readonly DependencyProperty ColorMapProperty = DependencyProperty.Register(
            "ColorMap",
            typeof(ColorMap),
            typeof(MatrixCanvasView),
            new PropertyMetadata(default(ColorMap), OnPropertyChanged));

        /// <summary>
        /// The color map order property.
        /// </summary>
        public static readonly DependencyProperty ColorMapOrderProperty = DependencyProperty.Register(
            "ColorMapOrder", typeof(ColorMapOrder), typeof(MatrixCanvasView), new PropertyMetadata(ColorMapOrder.Normal, OnPropertyChanged));

        /// <summary>
        /// The number of colors property.
        /// </summary>
        public static readonly DependencyProperty NumberOfColorsProperty = DependencyProperty.Register(
            "NumberOfColors", typeof(int), typeof(MatrixCanvasView), new PropertyMetadata(default(int), OnPropertyChanged));

        /// <summary>
        /// The show color bar property.
        /// </summary>
        public static readonly DependencyProperty ShowColorBarProperty = DependencyProperty.Register(
            "ShowColorBar",
            typeof(bool),
            typeof(MatrixCanvasView),
            new PropertyMetadata(default(bool), OnPropertyChanged));

        /// <summary>
        /// The data canvas background brush property.
        /// </summary>
        public static readonly DependencyProperty DataCanvasBackgroundBrushProperty =
            DependencyProperty.Register(
                "DataCanvasBackgroundBrush",
                typeof(SolidColorBrush),
                typeof(MatrixCanvasView),
                new PropertyMetadata(default(SolidColorBrush)));

        /// <summary>
        /// The mask opacity property.
        /// </summary>
        public static readonly DependencyProperty MaskOpacityProperty = DependencyProperty.Register(
            "MaskOpacity", typeof(double), typeof(MatrixCanvasView), new PropertyMetadata(default(double), OnPropertyChanged));

        /// <summary>
        /// The show title property.
        /// </summary>
        public static readonly DependencyProperty ShowTitleProperty = DependencyProperty.Register(
            "ShowTitle", typeof(bool), typeof(MatrixCanvasView), new PropertyMetadata(default(bool), OnPropertyChanged));

        /// <summary>
        /// The show tool tips property.
        /// </summary>
        public static readonly DependencyProperty ShowToolTipsProperty = DependencyProperty.Register(
            "ShowToolTips",
            typeof(bool),
            typeof(MatrixCanvasView),
            new PropertyMetadata(default(bool), OnPropertyChanged));

        /// <summary>
        /// The show cell text property.
        /// </summary>
        public static readonly DependencyProperty ShowCellTextProperty = DependencyProperty.Register(
            "ShowCellText",
            typeof(bool),
            typeof(MatrixCanvasView),
            new PropertyMetadata(default(bool), OnPropertyChanged));

        /// <summary>
        /// The grid cell size property.
        /// </summary>
        public static readonly DependencyProperty GridCellSizeProperty = DependencyProperty.Register(
            "GridCellSize", typeof(int), typeof(MatrixCanvasView), new PropertyMetadata(default(int), OnPropertyChanged));

        /// <summary>
        /// The cell border thickness property.
        /// </summary>
        public static readonly DependencyProperty CellBorderThicknessProperty =
            DependencyProperty.Register(
                "CellBorderThickness",
                typeof(double),
                typeof(MatrixCanvasView),
                new PropertyMetadata(default(double), OnPropertyChanged));

        /// <summary>
        /// The show diagnostics property.
        /// </summary>
        public static readonly DependencyProperty ShowDiagnosticsProperty =
            DependencyProperty.Register(
                "ShowDiagnostics", 
                typeof(bool), 
                typeof(MatrixCanvasView), 
                new PropertyMetadata(default(bool), OnPropertyChanged));

        #endregion

        #region private fields
        /// <summary>
        /// The small tick size.
        /// </summary>
        private const double SmallTickSize = 5;

        /// <summary>
        /// The large tick size.
        /// </summary>
        private const double LargeTickSize = 9;

        /// <summary>
        /// The bitmap rendering scale.
        /// </summary>
        private static double scale = 4;

        /// <summary>
        /// The max number of items that this control can display.
        /// </summary>
        private int maxItems = 3000;
        
        /// <summary>
        /// The view constraints.
        /// </summary>
        private ViewInformation viewConstraints;

        /// <summary>
        /// Render the grid as bitmap
        /// </summary>
        private bool renderGridAsBitmap;

        /// <summary>
        /// The color map brush container.
        /// </summary>
        private ColorMapBrushContainer colorMapBrushContainer;

        /// <summary>
        /// The view model.
        /// </summary>
        private MatrixCanvasViewModel viewModel = new MatrixCanvasViewModel { DataRange = new RealRange() };

        /// <summary>
        /// The mask.
        /// </summary>
        private int[][] mask;

        /// <summary>
        /// The mask legend items.
        /// </summary>
        private string[] maskLegendItems;

        /// <summary>
        /// The mask color map brush container.
        /// </summary>
        private ColorMapBrushContainer maskColorMapBrushContainer;

        /// <summary>
        /// Derive range from data
        /// </summary>
        private bool deriveRangeFromData = true;

        /// <summary>
        /// The show x axis label.
        /// </summary>
        private bool showXAxisLabel;

        /// <summary>
        /// The show y axis label.
        /// </summary>
        private bool showYAxisLabel;

        /// <summary>
        /// The x axis label.
        /// </summary>
        private string xAxisLabel = string.Empty;

        /// <summary>
        /// The y axis label.
        /// </summary>
        private string yAxisLabel = string.Empty;

        /// <summary>
        /// The show tick marks.
        /// </summary>
        private bool showTickMarks = true;

        /// <summary>
        /// The title.
        /// </summary>
        private string title;

        /// <summary>
        /// The show title.
        /// </summary>
        private bool showTitle = true;

        /// <summary>
        /// The use mask.
        /// </summary>
        private bool useMask = true;

        /// <summary>
        /// The mask color map.
        /// </summary>
        private ColorMap maskColorMap = ColorMap.Rainbow;

        /// <summary>
        /// The show mask legend.
        /// </summary>
        private bool showMaskLegend = true;

        /// <summary>
        /// Whether to round the range
        /// </summary>
        private bool roundRange = true;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MatrixCanvasView"/> class.
        /// </summary>
        public MatrixCanvasView()
        {
            InitializeComponent();
            this.ViewConstraints = new ViewInformation { MinimumSize = ViewSize.Cell };
            this.SetDefaults();
        }
        #endregion

        #region events
        /// <summary>
        /// The property changed event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Properties promoted to the top
        /// <summary>
        /// Gets or sets a value indicating whether show x axis label.
        /// </summary>
        [DisplayName(@"Show x-axis label")]
        public bool ShowXAxisLabel
        {
            get
            {
                return this.showXAxisLabel;
            }

            set
            {
                if (this.showXAxisLabel == value)
                {
                    return;
                }

                this.showXAxisLabel = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether show y axis label.
        /// </summary>
        [DisplayName(@"Show y-axis label")]
        public bool ShowYAxisLabel
        {
            get
            {
                return this.showYAxisLabel;
            }

            set
            {
                if (this.showYAxisLabel == value)
                {
                    return;
                }

                this.showYAxisLabel = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the x axis label.
        /// </summary>
        [DisplayName(@"x-axis label")]
        public string XAxisLabel
        {
            get
            {
                return this.xAxisLabel;
            }

            set
            {
                if (this.xAxisLabel == value)
                {
                    return;
                }

                this.xAxisLabel = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the Y axis label.
        /// </summary>
        [DisplayName(@"y-axis Label")]
        public string YAxisLabel
        {
            get
            {
                return this.yAxisLabel;
            }

            set
            {
                if (this.yAxisLabel == value)
                {
                    return;
                }

                this.yAxisLabel = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the grid cell size.
        /// </summary>
        [DisplayName(@"Grid Cell Size")]
        public int GridCellSize
        {
            get
            {
                return (int)GetValue(GridCellSizeProperty);
            }

            set
            {
                SetValue(GridCellSizeProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether show color bar.
        /// </summary>
        [DisplayName(@"Show Color Bar")]
        public bool ShowColorBar
        {
            get
            {
                return (bool)GetValue(ShowColorBarProperty);
            }

            set
            {
                SetValue(ShowColorBarProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether show tool tips.
        /// </summary>
        [DisplayName(@"Show ToolTips")]
        public bool ShowToolTips
        {
            get
            {
                return (bool)GetValue(ShowToolTipsProperty);
            }

            set
            {
                SetValue(ShowToolTipsProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether show tick marks.
        /// </summary>
        [DisplayName(@"Show Tick Marks")]
        public bool ShowTickMarks
        {
            get
            {
                return this.showTickMarks;
            }

            set
            {
                if (this.showTickMarks == value)
                {
                    return;
                }

                this.showTickMarks = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether show cell text.
        /// </summary>
        [DisplayName(@"Show Text in Cells")]
        public bool ShowCellText
        {
            get
            {
                return (bool)GetValue(ShowCellTextProperty);
            }

            set
            {
                SetValue(ShowCellTextProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the cell border thickness.
        /// </summary>
        [DisplayName(@"Cell border thickness")]
        public double CellBorderThickness
        {
            get
            {
                return (double)GetValue(CellBorderThicknessProperty);
            }

            set
            {
                SetValue(CellBorderThicknessProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the color map.
        /// </summary>
        [DisplayName(@"Color map")]
        public ColorMap ColorMap
        {
            get
            {
                return (ColorMap)GetValue(ColorMapProperty);
            }

            set
            {
                SetValue(ColorMapProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the color map order.
        /// </summary>
        [DisplayName(@"Color map order")]
        public ColorMapOrder ColorMapOrder
        {
            get
            {
                return (ColorMapOrder)GetValue(ColorMapOrderProperty);
            }

            set
            {
                SetValue(ColorMapOrderProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of items to display.
        /// </summary>
        /// <value>
        /// The maximum items.
        /// </value>
        [DisplayName(@"Maximum number of items to display")]
        public int MaxItems
        {
            get
            {
                return this.maxItems;
            }

            set
            {
                if (this.maxItems == value)
                {
                    return;
                }

                this.maxItems = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to derive range from data.
        /// </summary>
        [DisplayName(@"Derive range from data")]
        public bool DeriveRangeFromData
        {
            get
            {
                return this.deriveRangeFromData;
            }

            set
            {
                if (this.deriveRangeFromData == value)
                {
                    return;
                }

                this.deriveRangeFromData = value;
                if (this.deriveRangeFromData)
                {
                    this.viewModel = ArrayHelpers.ParseArray(DataContext, value, this.viewModel.DataRange);
                }
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [round range].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [round range]; otherwise, <c>false</c>.
        /// </value>
        [DisplayName(@"Round range to significant digits")]
        public bool RoundRange
        {
            get
            {
                return this.roundRange;
            }

            set
            {
                if (this.roundRange == value)
                {
                    return;
                }

                this.roundRange = value;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged(nameof(Range));
            }
        }

        /// <summary>
        /// Gets or sets the y minimum
        /// </summary>
        [DisplayName(@"Y Minimum")]
        public double YMin
        {
            get
            {
                return this.roundRange ? this.viewModel.DataRange.Round().Min : this.viewModel.DataRange.Min;
            }

            set
            {
                this.viewModel.DataRange.Min = value;
                this.DeriveRangeFromData = false;
                this.RoundRange = false;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the y maximum
        /// </summary>
        [DisplayName(@"Y Maximum")]
        public double YMax
        {
            get
            {
                return this.roundRange ? this.viewModel.DataRange.Round().Max : this.viewModel.DataRange.Max;
            }

            set
            {
                this.viewModel.DataRange.Max = value;
                this.DeriveRangeFromData = false;
                this.RoundRange = false;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the row sort container.
        /// </summary>
        [DisplayName(@"Row sorting")]
        public SortContainer RowSortContainer
        {
            get
            {
                return this.viewModel.RowSorter;
            }

            set
            {
                if (this.viewModel.RowSorter == value)
                {
                    return;
                }

                this.viewModel.RowSorter = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the column sort direction.
        /// </summary>
        [DisplayName(@"Column sorting")]
        public SortContainer SortContainer
        {
            get
            {
                return this.viewModel.Sorter;
            }

            set
            {
                if (this.viewModel.Sorter == value)
                {
                    return;
                }

                this.viewModel.Sorter = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the scale.
        /// </summary>
        [DisplayName(@"Bitmap Rendering Scale")]
        public double Scale
        {
            get
            {
                return scale;
            }

            set
            {
                scale = value;
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
        /// Gets or sets a value indicating whether show title.
        /// </summary>
        [DisplayName(@"Show Title")]
        public bool ShowTitle
        {
            get
            {
                return this.showTitle;
            }

            set
            {
                if (this.showTitle == value)
                {
                    return;
                }

                this.showTitle = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether use mask.
        /// </summary>
        [DisplayName(@"Use Mask")]
        public bool UseMask
        {
            get
            {
                return this.useMask;
            }

            set
            {
                if (this.useMask == value)
                {
                    return;
                }

                this.useMask = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the mask opacity.
        /// </summary>
        [DisplayName(@"Mask opacity")]
        public double MaskOpacity
        {
            get
            {
                return (double)GetValue(MaskOpacityProperty);
            }

            set
            {
                if (value < 0.0 || value > 1.0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                SetValue(MaskOpacityProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the mask color map.
        /// </summary>
        [DisplayName(@"Mask color map")]
        public ColorMap MaskColorMap
        {
            get
            {
                return this.maskColorMap;
            }

            set
            {
                if (this.maskColorMap == value)
                {
                    return;
                }

                this.maskColorMap = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether show mask legend.
        /// </summary>
        [DisplayName(@"Show Mask Legend")]
        public bool ShowMaskLegend
        {
            get
            {
                return this.showMaskLegend;
            }

            set
            {
                if (this.showMaskLegend == value)
                {
                    return;
                }

                this.showMaskLegend = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether show diagnostics.
        /// </summary>
        [DisplayName(@"Show Diagnostics")]
        public bool ShowDiagnostics
        {
            get
            {
                return (bool)GetValue(ShowDiagnosticsProperty);
            }

            set
            {
                SetValue(ShowDiagnosticsProperty, value);
            }
        }

        /// <summary>
        /// Gets the range.
        /// </summary>
        public RealRange Range
        {
            get
            {
                return this.roundRange ? this.viewModel.DataRange.Round() : this.viewModel.DataRange;
            }
        }
        #endregion

        #region IDataExpressionContext Members
        /// <summary>
        /// Gets or sets the data expression.
        /// </summary>
        public IExpressionPart DataExpression { get; set; }
        #endregion
        
        #region IConstrainableView Members
        /// <summary>
        /// Gets or sets the view constraints.
        /// </summary>
        public ViewInformation ViewConstraints 
        {
            get
            {
                return this.viewConstraints;
            }

            set
            {
                this.viewConstraints = value;
                this.renderGridAsBitmap = this.viewConstraints.OffScreenRendering;

                if (value.MaximumSize == ViewSize.Cell)
                {
                    this.StackCell.Visibility = Visibility.Visible;
                    this.StackFull.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.StackCell.Visibility = Visibility.Collapsed;
                    this.StackFull.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Gets or sets the data canvas background brush.
        /// </summary>
        [DisplayName(@"Data Canvas Background Brush")]
        public SolidColorBrush DataCanvasBackgroundBrush
        {
            get
            {
                return (SolidColorBrush)GetValue(DataCanvasBackgroundBrushProperty);
            }

            set
            {
                SetValue(DataCanvasBackgroundBrushProperty, value);
            }
        }

        #endregion

        #region public functions
        /// <summary>
        /// Indicates whether the <see cref="P:System.Windows.Controls.ContentControl.Content" /> property should be persisted.
        /// </summary>
        /// <returns>
        /// true if the <see cref="P:System.Windows.Controls.ContentControl.Content" /> property should be persisted; otherwise, false.
        /// </returns>
        public override bool ShouldSerializeContent()
        {
            return false;
        }
        #endregion

        #region private static functions
        /// <summary>
        /// The on property changed handler.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="e">The e.</param>
        /// <exception cref="System.NullReferenceException">canvas View</exception>
        /// <exception cref="NullReferenceException"></exception>
        private static void OnPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            MatrixCanvasView canvasView = source as MatrixCanvasView;
            if (canvasView == null)
            {
                throw new NullReferenceException("canvasView");
            }

            if (e.Property.Name != null)
            {
                canvasView.NotifyPropertyChanged(e.Property.Name);
            }
            else
            {
                canvasView.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Draws the line on panel.
        /// </summary>
        /// <param name="panel">The panel.</param>
        /// <param name="x1">The x1.</param>
        /// <param name="x2">The x2.</param>
        /// <param name="y1">The y1.</param>
        /// <param name="y2">The y2.</param>
        /// <param name="strokeThickness">The stroke thickness.</param>
        private static void DrawLineOnPanel(Panel panel, double x1, double x2, double y1, double y2, double strokeThickness = 1.0)
        {
            panel.Children.Add(new Line { Stroke = Brushes.Black, X1 = x1, X2 = x2, Y1 = y1, Y2 = y2, StrokeThickness = strokeThickness });
        }

        /// <summary>
        /// Gets the tool tip.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <param name="j">The j.</param>
        /// <param name="element">The element.</param>
        /// <returns>The tool tip</returns>
        private static string GetToolTip(int i, int j, object element)
        {
            return element == null
                       ? "[" + i + "," + j + "] = " + "(null)"
                       : "[" + i + "," + j + "]=" + ObjectToStringConverter.ToDisplayString(element);
        }

/*
        /// <summary>
        /// Adds the item to canvas.
        /// </summary>
        /// <param name="canvas">The canvas.</param>
        private static void AddItemToCanvas(Border canvas)
        {
            // No data provided, create cross hatch brush
            const double BrushScale = 10;

            Canvas visualBrushCanvas = new Canvas();
            visualBrushCanvas.Children.Add(new Rectangle { Fill = Brushes.Transparent, Width = BrushScale, Height = BrushScale });
            visualBrushCanvas.Children.Add(new Path
            {
                Stroke = Brushes.Red,
                Data = Geometry.Parse(string.Format("M 0 0 l {0} {0}", BrushScale))
            });
            visualBrushCanvas.Children.Add(new Path
            {
                Stroke = Brushes.Red,
                Data = Geometry.Parse(string.Format("M 0 {0} l {0} -{0}", BrushScale))
            });

            VisualBrush visualBrush = new VisualBrush
            {
                TileMode = TileMode.Tile,
                ViewportUnits = BrushMappingMode.RelativeToBoundingBox,
                Viewbox = new Rect(0, 0, BrushScale, BrushScale),
                ViewboxUnits = BrushMappingMode.Absolute,
                Visual = visualBrushCanvas
            };

            canvas.Background = visualBrush;
        }
*/

        /// <summary>
        /// Renders the control as bitmap.
        /// </summary>
        /// <param name="parentPanel">The parent panel.</param>
        /// <param name="controlToRender">The control to render.</param>
        /// <param name="renderScale">The bitmap rendering scale.</param>
        /// <exception cref="System.InvalidOperationException">0 height</exception>
        private static void RenderControlAsBitmap(Panel parentPanel, FrameworkElement controlToRender, double renderScale)
        {
            controlToRender.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            controlToRender.Arrange(new Rect(controlToRender.DesiredSize));
            controlToRender.UpdateLayout();

            if (double.IsNaN(controlToRender.ActualHeight) || (int)controlToRender.ActualHeight == 0)
            {
                throw new InvalidOperationException("0 height");
            }

            // Scale according to A4 width/height
            ////double scaleWidth = 794.0 / controlToRender.ActualWidth;
            ////double scaleHeight = 1123.0 / controlToRender.ActualHeight;

            ////double scale = Math.Min(scaleWidth, scaleHeight);

            int width = (int)Math.Round(renderScale * controlToRender.ActualWidth);
            int height = (int)Math.Round(renderScale * controlToRender.ActualHeight);

            try
            {
                RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96 * renderScale, 96 * renderScale, PixelFormats.Pbgra32);
                bitmap.Render(controlToRender);

                while (parentPanel.Children.Count > 1)
                {
                    parentPanel.Children.RemoveAt(1);
                }

                parentPanel.Children.Add(
                    new Image
                    {
                        Source = bitmap,
                        Width = controlToRender.ActualWidth,
                        Height = controlToRender.ActualHeight
                    });

                // Console.WriteLine("RenderTargetBitmap Width: {0}, Height {1}, Scale {2}", width, height, Scale);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }
        #endregion

        /// <summary>
        /// Sets the title by data expression.
        /// </summary>
        private void SetTitleByDataExpression()
        {
            if (this.DataExpression != null)
            {
                this.title = this.DataExpression.DisplayText;
            }
        }

        /// <summary>
        /// Data context changed on the user control.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void UserControlDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            object obj = DataContext;
            if (obj is IExpressionPart)
            {
                obj = ((IExpressionPart)obj).Value;
            }

            MaskedMatrix maskedMatrix = obj as MaskedMatrix;
            if (maskedMatrix != null && maskedMatrix.IsValid() == MaskedMatrixValidity.Valid)
            {
                this.mask = maskedMatrix.Mask;
                this.maskLegendItems = maskedMatrix.MaskLabels;
                obj = maskedMatrix.Data;
            }

            this.viewModel = ArrayHelpers.ParseArray(
                obj,
                this.deriveRangeFromData,
                this.viewModel == null ? null : this.viewModel.DataRange);
            this.BuildView();
        }

        /// <summary>
        /// Sets the defaults.
        /// </summary>
        private void SetDefaults()
        {
            MaskLegendStackPanel.Visibility = Visibility.Collapsed;
            this.ShowDiagnostics = false;
            this.MaskOpacity = 1.0;

            this.DataCanvasBackgroundBrush = Brushes.Transparent;

            const double T = 0.5;
            const int S = 10;
            if (this.viewModel.Elements > 0)
            {
                this.CellBorderThickness = this.viewModel.Elements > 2000 ? 0.0 : T;
                this.GridCellSize = S; // Math.Min(S, Math.Max(1000 / Math.Max(this.viewModel.Rows, this.viewModel.Cols), 1)); //// 20;
            }
            else
            {
                this.CellBorderThickness = T;
                this.GridCellSize = S;
            }

            this.ShowToolTips = false;
            this.ColorMap = ColorMap.GrayBuffered;
            //// this.NumberOfColorMapColors = this.dataType == TypeCode.Boolean ? 2 : 11;

#if TESTING
            this.YAxisLabel = "y-axis label";
            this.XAxisLabel = "x-axis label";
            this.ShowYAxisLabel = true;
            this.ShowXAxisLabel = true;
            this.Title = "Testing";
            this.ShowTitle = true;
            this.ShowCellText = true;
            this.showMaskLegend = true;
            this.ShowColorBar = true;
#endif
            /*
            if (data.nRows < 20 && data.nCols < 20)
            {
                showCellText = true;
                showToolTips = true;
                showGridLines = true;
            }
            else
            {
                showCellText = false;
                showToolTips = false;
                showGridLines = false;
            }*/
        }

        /// <summary>
        /// Builds the view.
        /// </summary>
        private void BuildView()
        {
            if (this.GridCellSize == 0)
            {
                this.SetDefaults();
            }

            if (string.IsNullOrEmpty(this.Title))
            {
                this.SetTitleByDataExpression();
            }

            if (this.viewModel == null || this.viewModel.DataType == TypeCode.Empty || this.viewModel.Data == null)
            {
                return;
            }

            // TODO: Check that rows/columns have been set before determining cell border thickness
            ////this.CellBorderThickness = (this.rows * this.columns) > 200 ? 0.0 : 0.5;

            this.ResetColorMaps();

            if (this.viewModel.Rows * this.viewModel.Cols < this.maxItems)
            {
                this.DataCanvasGrid.Width = this.viewModel.Cols * this.GridCellSize;
                this.DataCanvasGrid.Height = this.viewModel.Rows * this.GridCellSize;

                this.DrawDataCanvasGrid();
                this.DrawCellGrid();
            }
            else
            {
                // TODO: Draw something else, or at least write a message. Possibly remove this?
                this.DataCanvasGrid.Children.Clear();
                this.DataCanvasGrid.Children.Add(
                    new TextBlock
                        {
                            Text =
                                string.Format(
                                    "Too many items to display (max = {0})\nRows: {1}\nColumns: {2}\nElements: {3}\nData type: {4}",
                                    this.maxItems,
                                    this.viewModel.Rows,
                                    this.viewModel.Cols,
                                    this.viewModel.Elements,
                                    this.viewModel.DataType),
                            FontSize = 20,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Center
                        });
                this.CellGrid.Children.Clear();
                this.CellGrid.Children.Add(
                    new TextBlock
                        {
                            Text =
                                string.Format(
                                    "Rows: {0}\nColumns: {1}\nData type: {2}",
                                    this.viewModel.Rows,
                                    this.viewModel.Cols,
                                    this.viewModel.DataType),
                            FontSize = 10,
                            VerticalAlignment = VerticalAlignment.Center
                        });
            }

            if (this.UseMask && this.mask != null)
            {
                this.MaskLegendStackPanel.Visibility = Visibility.Visible;

                ////int splits = this.mask.Select(ia => ia.Max()).Max() + 2;
                this.DrawMaskLegend();
            }

            if (this.renderGridAsBitmap)
            {
                RenderControlAsBitmap(LayerGrid, DataCanvasGrid, this.Scale);

                ////if (this.ShowMaskLegend && this.mask != null)
                ////{
                ////    ////this.MaskLegendStackPanel.Width = this.DataCanvasGrid.Width;
                ////    ////this.MaskLegendStackPanel.Height = this.GridCellSize + (this.CellBorderThickness * 2);
                ////    RenderControlAsBitmap(MaskLegendLayerGrid, MaskLegendStackPanel, this.Scale);
                ////}
            }

            if (this.ShowColorBar)
            {
                this.DrawColorBar();
                this.DrawColorBarTicksAndText();
            }

            TitleBlock.Margin = string.IsNullOrEmpty(this.Title) ? new Thickness(0) : new Thickness(5);

            if (!this.ShowDiagnostics)
            {
                return;
            }

            IList<string> diagnostics = new List<string>
                                            {
                                                string.Format("Rows: {0}", this.viewModel.Rows),
                                                string.Format("Columns: {0}", this.viewModel.Cols),
                                                string.Format("Elements: {0}", this.viewModel.Elements),
                                                string.Format("Total brushes: {0}", this.colorMapBrushContainer.CacheSize),
                                            };

            Dictionary<Type, int> typeCounts = new Dictionary<Type, int>();
            this.CountVisualTreeByType(this.DataCanvasGrid, ref typeCounts);
            diagnostics.Add("Type Counts");
            foreach (KeyValuePair<Type, int> kvp in typeCounts)
            {
                diagnostics.Add(string.Format("\t{0}: {1}", kvp.Key, kvp.Value));
            }

            this.Diagnostics.Text = string.Join("\n", diagnostics);
        }

        /// <summary>
        /// Counts the visual tree by type.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="typeCounts">The type counts.</param>
        private void CountVisualTreeByType(DependencyObject obj, ref Dictionary<Type, int> typeCounts)
        {
            if (obj == null)
            {
                return;
            }

            Type t = obj.GetType();
            if (!typeCounts.ContainsKey(t))
            {
                typeCounts[t] = 0;
            }

            typeCounts[t]++;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            { 
                this.CountVisualTreeByType(VisualTreeHelper.GetChild(obj, i), ref typeCounts);
            }
        }

        /// <summary>
        /// Resets the color maps.
        /// </summary>
        private void ResetColorMaps()
        {
            this.colorMapBrushContainer = new ColorMapBrushContainer(this.ColorMap, this.Range) { ColorMapOrder = this.ColorMapOrder };
            if (this.useMask && this.mask != null)
            {
                this.maskColorMapBrushContainer = new ColorMapBrushContainer(this.maskColorMap, this.maskLegendItems.Length);
            }
        }

        /// <summary>
        /// Draws the cell grid.
        /// </summary>
        private void DrawCellGrid()
        {
            CellGrid.Rows = this.viewModel.Rows;
            CellGrid.Columns = this.viewModel.Cols;
            CellGrid.Children.Clear();
            foreach (int i in this.viewModel.GetRowEnumerator())
            {
                foreach (int j in this.viewModel.GetColumnEnumerator())
                {
                    object obj = ArrayHelpers.GetValue(this.viewModel.Data, i, j, this.viewModel.Cols);

                    if (obj == null)
                    {
                      //  continue;
                    }

                    CellGrid.Children.Add(
                        new Rectangle
                        {
                            Width = 90.0 / this.viewModel.Cols,
                            Height = 90.0 / this.viewModel.Rows,
                            //// Fill = (obj == null) || Math.Abs(d - 0.0) < double.Epsilon ? Brushes.Transparent : this.colorMapBrushContainer.GetBrush(d)
                            Fill = obj == null ? Brushes.Purple : this.colorMapBrushContainer.GetBrush(Convert.ToDouble(obj))
                            // Fill = this.colorMapBrushContainer.GetBrush(Convert.ToDouble(obj))
                        });
                }
            }
        }

        /// <summary>
        /// Draws the data canvas grid.
        /// </summary>
        private void DrawDataCanvasGrid()
        {
            ////DataCanvasGrid.Rows = this.rows;
            ////DataCanvasGrid.Columns = this.columns;
            DataCanvasGrid.Children.Clear();
            
            if (this.ShowTickMarks)
            {
                XTicks1.Height = 10;
                XTicks1.Width = this.GridCellSize * this.viewModel.Cols;
                XTicks1.Children.Clear();
                XTicks2.Height = 10;
                XTicks2.Width = this.GridCellSize * this.viewModel.Cols;
                XTicks2.Children.Clear();
                YTicks1.Width = 10;
                YTicks1.Height = this.GridCellSize * this.viewModel.Rows;
                YTicks1.Children.Clear();
                YTicks2.Width = 10;
                YTicks2.Height = this.GridCellSize * this.viewModel.Rows;
                YTicks2.Children.Clear();
            }

            double t = this.CellBorderThickness * 2;
            double h = (this.viewModel.Rows * this.GridCellSize) + this.CellBorderThickness;
            double w = (this.viewModel.GetRowLength(0) * this.GridCellSize) + this.CellBorderThickness;

            DrawLineOnPanel(this.DataCanvasGrid, -this.CellBorderThickness, w, 0, 0, t);
            DrawLineOnPanel(this.DataCanvasGrid, 0, 0, -this.CellBorderThickness, h, t);

            foreach (int i in this.viewModel.GetRowEnumerator())
            {
                var maxCols = this.viewModel.GetRowLength(i);
                if (i < this.viewModel.Rows-1) maxCols = Math.Max(maxCols, this.viewModel.GetRowLength(i + 1));
                w = (maxCols * this.GridCellSize) + this.CellBorderThickness;

                // draw horizontal gridline
                DrawLineOnPanel(this.DataCanvasGrid, 0, w, (i + 1) * this.GridCellSize, (i + 1) * this.GridCellSize, t);
                
                foreach (int j in this.viewModel.GetColumnEnumerator())
                {
                    // Draw vertical gridline
                    if (i == 0)
                    {
                        DrawLineOnPanel(this.DataCanvasGrid, (j + 1) * this.GridCellSize, (j + 1) * this.GridCellSize, 0, h, t);
                    }

                    object value = ArrayHelpers.GetValue(this.viewModel.Data, i, j, this.viewModel.Cols);
                    this.AddElementToDataCanvasGrid(i, j, value);

                    if (this.useMask && this.mask != null)
                    {
                        this.AddItemToPanel(this.DataCanvasGrid, i, j, value, this.mask[j]);
                    }

                    this.DrawTickMark(i, j);                    
                }
            }

            // Add final ticks to the last row/column if needed
            this.DrawTickMark(this.viewModel.Rows, 0);
            this.DrawTickMark(0, this.viewModel.Cols);
        }

        /// <summary>
        /// Draws the tick mark.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="col">The col.</param>
        private void DrawTickMark(int row, int col)
        {
            if (col == 0 && row % 5 == 0)
            {
                double verticalPosition = row * this.GridCellSize;
                
                // Draw horizontal tick
                DrawLineOnPanel(this.YTicks1, (row % 10 == 0) ? 1 : SmallTickSize, LargeTickSize, verticalPosition, verticalPosition);

                // Also draw on the right
                DrawLineOnPanel(this.YTicks2, 1, (row % 10 == 0) ? LargeTickSize : SmallTickSize, verticalPosition, verticalPosition);
            }

            if (row == 0 && col % 5 == 0)
            {
                double horizontalPosition = col * this.GridCellSize;

                // Draw vertical tick
                DrawLineOnPanel(this.XTicks1, horizontalPosition, horizontalPosition, (col % 10 == 0) ? 1 : SmallTickSize, LargeTickSize);

                // Also draw on the bottom
                DrawLineOnPanel(this.XTicks2, horizontalPosition, horizontalPosition, 1, (col % 10 == 0) ? LargeTickSize : SmallTickSize);
            }
        }
        
        /// <summary>
        /// Draws the color bar.
        /// </summary>
        private void DrawColorBar()
        {
            ColorBarCanvas.Children.Clear();
            if (this.viewModel.DataType == TypeCode.Boolean)
            {
                // For Boolean Want two blocks of color rather than the gradient
                ColorBarCanvas.Children.Add(new Rectangle
                    {
                        Width = ColorBarCanvas.Width,
                        Height = ColorBarCanvas.Height / 2,
                        Fill = new SolidColorBrush(this.colorMapBrushContainer.EndColor)
                    });
                ColorBarCanvas.Children.Add(new Rectangle
                    {
                        Width = ColorBarCanvas.Width,
                        Height = ColorBarCanvas.Height / 2,
                        Fill = new SolidColorBrush(this.colorMapBrushContainer.StartColor),
                        Margin = new Thickness(0, ColorBarCanvas.Height / 2, 0, 0)
                    });
            }
            else
            {
                ColorBarCanvas.Children.Add(new Rectangle
                    {
                        Height = ColorBarCanvas.Height, 
                        Width = ColorBarCanvas.Width,
                        Fill = this.colorMapBrushContainer.GetGradientBrush(Orientation.Vertical)
                    });
            }
        }

        /// <summary>
        /// Draws the color bar ticks and text.
        /// </summary>
        private void DrawColorBarTicksAndText()
        {
            double[] quantiles = this.colorMapBrushContainer.Quantiles;

            ColorBarTicks.Children.Clear();
            ColorBarText.Children.Clear();

            double height = ColorBarCanvas.Height + (ColorBarBorder.BorderThickness.Top / 2) + (ColorBarBorder.BorderThickness.Bottom / 2);

            for (int i = 0; i < quantiles.Length; i++)
            {
                double yShift = ((quantiles.Length - i - 1) * (height / (quantiles.Length - 1))) - 0.5;

                // Draw the tick lines
                Line line = new Line { Stroke = Brushes.Black, X1 = 0, X2 = 5, Y1 = yShift, Y2 = yShift };

                // Write the values as text
                TextBlock tb = new TextBlock
                    {
                        Text = this.viewModel.DataType == TypeCode.Boolean ? (i == 1).ToString() : string.Format("{0,5:0.##}", quantiles[i]),
                        Margin = new Thickness(5, yShift - 9, 0, 0)
                    };

                ColorBarTicks.Children.Add(line);
                ColorBarText.Children.Add(tb);
            }
        }

        /// <summary>
        /// The add text to control.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// "The TextBlock."
        /// </returns>
        private TextBlock CreateTextBlock(object value)
        {
            // Add the actual value as a text block
            TextBlock tb = new TextBlock
            {
                Text = ObjectToStringConverter.ToDisplayString(value),
                FontSize = this.GridCellSize / 4.0
            };

            // If we're using the Gray colormap, use white text when more than 50% gray
            if (this.ColorMap == ColorMap.Gray && Convert.ToDouble(value) < this.Range.Delta / 2)
            {
                tb.Foreground = Brushes.White;
            }

            return tb;
        }

        /// <summary>
        /// Adds the element to data canvas grid.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <param name="j">The j.</param>
        /// <param name="element">The element.</param>
        private void AddElementToDataCanvasGrid(int i, int j, object element)
        {
            bool isEmpty = element == null; 
            Rectangle rect = new Rectangle
                                 {
                                     Width = this.GridCellSize - (this.CellBorderThickness * 2) + (isEmpty? this.CellBorderThickness*4:0),
                                     Height = this.GridCellSize - (this.CellBorderThickness * 2),
                                     Fill =
                                         isEmpty
                                             ? Brushes.White
                                             : this.colorMapBrushContainer.GetBrush(Convert.ToDouble(element)),
                                     SnapsToDevicePixels = true
                                 };

            Canvas.SetLeft(rect, (j * this.GridCellSize) + this.CellBorderThickness);
            Canvas.SetTop(rect, (i * this.GridCellSize) + this.CellBorderThickness);
            this.DataCanvasGrid.Children.Add(rect);

            // Add tooltip, so long as there aren't too many rows and columns
            if (this.ShowToolTips)
            {
                rect.ToolTip = GetToolTip(i, j, element);
            }

            if (this.ShowCellText)
            {
                var tb = this.CreateTextBlock(element);
                Canvas.SetLeft(tb, (j * this.GridCellSize) + this.CellBorderThickness);
                Canvas.SetTop(tb, (i * this.GridCellSize) + this.CellBorderThickness);
                this.DataCanvasGrid.Children.Add(tb);
            }
        }

        /// <summary>
        /// Adds the item to panel.
        /// </summary>
        /// <param name="canvas">The canvas.</param>
        /// <param name="i">The row index.</param>
        /// <param name="j">The column index.</param>
        /// <param name="element">The element.</param>
        /// <param name="valueMask">The value mask.</param>
        private void AddItemToPanel(Canvas canvas, int i, int j, object element, IList<int> valueMask)
        {
            int vml = valueMask.Count;

            double w = this.GridCellSize - (this.CellBorderThickness * 2);
            double h = this.GridCellSize - (this.CellBorderThickness * 2);

            for (int index = 0; index < valueMask.Count; index++)
            {
                // todo: Change (bool)element to be a settable property
                if (this.viewModel.DataType == TypeCode.Boolean && (bool)element)
                {
                    continue;
                }

                var brush = this.maskColorMapBrushContainer.GetBrush(valueMask[index]);
                Rectangle rect = new Rectangle
                                     {
                                         Fill = brush,
                                         Height = h / vml,
                                         Width = w,
                                         Opacity = this.MaskOpacity
                                     };

                Canvas.SetLeft(rect, (j * this.GridCellSize) + this.CellBorderThickness);
                Canvas.SetTop(rect, (i * this.GridCellSize) + this.CellBorderThickness + (index * h / vml));
                canvas.Children.Add(rect);
            }
        }

        /// <summary>
        /// Draws the mask legend.
        /// </summary>
        private void DrawMaskLegend()
        {
            if (this.ShowMaskLegend == false || this.maskLegendItems == null)
            {
                this.MaskLegendStackPanel.Visibility = Visibility.Collapsed;
                return;
            }

            this.MaskLegendStackPanel.Visibility = Visibility.Visible;

            MaskLegendStackPanel.Children.Clear();

            double legendCellSize = this.GridCellSize;
            double legendBorderThickness = this.CellBorderThickness * 2;

            for (int i = 0; i < this.maskLegendItems.Length; i++)
            {
                string label = this.maskLegendItems[i];
                Brush brush = this.maskColorMapBrushContainer.GetBrush(i);

                Panel panel = new Grid { Margin = new Thickness(2) };

                Border border = new Border
                                    {
                                        BorderBrush = Brushes.Black,
                                        BorderThickness = new Thickness(legendBorderThickness),
                                        Height = legendCellSize,
                                        Width = legendCellSize,
                                        Background = brush
                                    };

                panel.Children.Add(border);

                if (this.renderGridAsBitmap)
                {
                    RenderControlAsBitmap(panel, border, this.Scale);
                }

                // Create text label in column 1
                TextBlock tb = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2) };

                StackPanel stackPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2) };

                // stackPanel.Children.Add(border);
                stackPanel.Children.Add(panel);
                stackPanel.Children.Add(tb);

                this.MaskLegendStackPanel.Children.Add(stackPanel);
            }
        }
        
        #region INotifyPropertyChanged Members
        /// <summary>
        /// Notify property changed.
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
