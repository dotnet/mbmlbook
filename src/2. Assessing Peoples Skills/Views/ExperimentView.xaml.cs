// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AssessingPeoplesSkills.Views
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows;

    using Microsoft.Research.Glo;

    using MBMLViews;
    using MBMLViews.Views;

    #region enums
    /// <summary>
    /// The sort rows by view.
    /// </summary>
    public enum SortRowsByView
    {
        /// <summary>
        /// No sorting
        /// </summary>
        None,

        /// <summary>
        /// Sort by responses.
        /// </summary>
        Responses,

        /// <summary>
        /// Sort by stated skills.
        /// </summary>
        StatedSkills,

        /// <summary>
        /// Sort by skills posterior means.
        /// </summary>
        SkillsPosteriorMeans
    }

    /// <summary>
    /// The sort columns by view.
    /// </summary>
    public enum SortColumnsByView
    {
        /// <summary>
        /// No sorting.
        /// </summary>
        None,

        /// <summary>
        /// Sort by skills for questions.
        /// </summary>
        SkillsForQuestions,

        /// <summary>
        /// Sort by responses.
        /// </summary>
        Responses,

        /// <summary>
        /// Sort by guess posterior means.
        /// </summary>
        GuessPosteriorMeans
    }
    #endregion

    /// <summary>
    /// Custom view for AssessingPeoplesSkills project
    /// </summary>
    [ViewInformation(TargetType = typeof(Experiment), Priority = 5, MinimumSize = ViewSize.LargePanel)]
    [Feature(Description = "Custom LearningSkills Experiment view", Date = "11/03/2013")]
    public partial class ExperimentView : IConstrainableView, INotifyPropertyChanged
    {
        /// <summary>
        /// The cell border thickness.
        /// </summary>
        private double cellBorderThickness = 1.0;

        /// <summary>
        /// The grid cell size.
        /// </summary>
        private int gridCellSize = 15;

        /// <summary>
        /// The show color bar.
        /// </summary>
        private bool showColorBar;

        /// <summary>
        /// The show tool tips.
        /// </summary>
        private bool showToolTips = true;
       
        /// <summary>
        /// The show cell text
        /// </summary>
        private bool showCellText;
        
        /// <summary>
        /// The color map
        /// </summary>
        private ColorMap colorMap = MBMLViews.ColorMap.Gray;

        /// <summary>
        /// The number of color map colors.
        /// </summary>
        private int numberOfColorMapColors = 11;

        /// <summary>
        /// The sort rows by view.
        /// </summary>
        private SortRowsByView sortRowsByView = SortRowsByView.None;

        /// <summary>
        /// The sort columns by view.
        /// </summary>
        private SortColumnsByView sortColumnsByView = SortColumnsByView.None;

        /// <summary>
        /// The row sort type.
        /// </summary>
        private SortType rowSortType = SortType.None;

        /// <summary>
        /// The column sort type.
        /// </summary>
        private SortType columnSortType = SortType.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExperimentView"/> class.
        /// </summary>
        public ExperimentView()
        {
            InitializeComponent();
            this.ViewConstraints = new ViewInformation { MinimumSize = ViewSize.SmallPanel };
        }

        /// <summary>
        /// The property changed event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #region Properties promoted to the top
        /// <summary>
        /// Gets or sets the cell border thickness.
        /// </summary>
        [DisplayName("Cell Border Thickness")]
        public double CellBorderThickness
        {
            get
            {
                return this.cellBorderThickness;
            }

            set
            {
                if (Math.Abs(this.cellBorderThickness - value) < double.Epsilon)
                {
                    return;
                }

                this.cellBorderThickness = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the grid cell size.
        /// </summary>
        [DisplayName("Grid Cell Size")]
        public int GridCellSize
        {
            get
            {
                return this.gridCellSize;
            }

            set
            {
                if (this.gridCellSize == value)
                {
                    return;
                }

                this.gridCellSize = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show the color bar.
        /// </summary>
        [DisplayName("Show Color Bar")]
        public bool ShowColorBar
        {
            get
            {
                return this.showColorBar;
            }

            set
            {
                if (this.showColorBar == value)
                {
                    return;
                }

                this.showColorBar = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show tool tips.
        /// </summary>
        [DisplayName("Show Tool Tips")]
        public bool ShowToolTips
        {
            get
            {
                return this.showToolTips;
            }

            set
            {
                if (this.showToolTips == value)
                {
                    return;
                }

                this.showToolTips = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show cell text.
        /// </summary>
        [DisplayName("Show Cell Text")]
        public bool ShowCellText
        {
            get
            {
                return this.showCellText;
            }

            set
            {
                if (this.showCellText == value)
                {
                    return;
                }

                this.showCellText = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the color map.
        /// </summary>
        [DisplayName("Color Map")]
        public ColorMap ColorMap
        {
            get
            {
                return this.colorMap;
            }

            set
            {
                if (this.colorMap == value)
                {
                    return;
                }

                this.colorMap = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the number of color map colors.
        /// </summary>
        [DisplayName("Number of color map colors")]
        public int NumberOfColorMapColors
        {
            get
            {
                return this.numberOfColorMapColors;
            }

            set
            {
                if (this.numberOfColorMapColors == value)
                {
                    return;
                }

                this.numberOfColorMapColors = value;
                this.NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the sort rows by view.
        /// </summary>
        [DisplayName("Sort rows by view")]
        public SortRowsByView SortRowsByView
        {
            get
            {
                return this.sortRowsByView;
            }

            set
            {
                if (this.sortRowsByView == value)
                {
                    return;
                }

                this.sortRowsByView = value;
                this.BuildView();
            }
        }

        /// <summary>
        /// Gets or sets the sort columns by view.
        /// </summary>
        [DisplayName("Sort columns by view")]
        public SortColumnsByView SortColumnsByView
        {
            get
            {
                return this.sortColumnsByView;
            }

            set
            {
                if (this.sortColumnsByView == value)
                {
                    return;
                }

                this.sortColumnsByView = value;
                this.BuildView();
            }
        }

        /// <summary>
        /// Gets or sets the row sort type.
        /// </summary>
        [DisplayName("Row sort type")]
        public SortType RowSortType
        {
            get
            {
                return this.rowSortType;
            }

            set
            {
                if (this.rowSortType == value)
                {
                    return;
                }

                this.rowSortType = value;
                this.BuildView();
            }
        }

        /// <summary>
        /// Gets or sets the column sort type.
        /// </summary>
        [DisplayName("Column sort type")]
        public SortType ColumnSortType
        {
            get
            {
                return this.columnSortType;
            }

            set
            {
                if (this.columnSortType == value)
                {
                    return;
                }

                this.columnSortType = value;
                this.BuildView();
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
        }
        #endregion

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
            this.BuildView();
        }

        /// <summary>
        /// Builds the view.
        /// </summary>
        private void BuildView()
        {
            var experiment = DataContext as Experiment;
            if (experiment == null)
            {
                return;
            }

            // Set the sort order
            ////this.SetRowSortOrder();
            ////this.SetColumnSortOrder();
        }

        /////// <summary>
        /////// Sets the row sort order.
        /////// </summary>
        ////private void SetRowSortOrder()
        ////{
        ////    var allViews = new[] { IsCorrectView, StatedSkillsView, SkillsPosteriorMeansView };

        ////    switch (SortRowsByView)
        ////    {
        ////        case SortRowsByView.None:
        ////            foreach (MatrixCanvasView view in allViews)
        ////            {
        ////                view.RowSortType = SortType.None;
        ////                view.RowSortDirection = SortDirection.Ascending;
        ////            }

        ////            break;
        ////        case SortRowsByView.Responses:
        ////            this.SwitchRowSortOrder(IsCorrectView, allViews.Except(new[] { IsCorrectView }));
        ////            break;
        ////        case SortRowsByView.SkillsPosteriorMeans:
        ////            this.SwitchRowSortOrder(
        ////                SkillsPosteriorMeansView, allViews.Except(new[] { SkillsPosteriorMeansView }));
        ////            break;
        ////        case SortRowsByView.StatedSkills:
        ////            this.SwitchRowSortOrder(StatedSkillsView, allViews.Except(new[] { StatedSkillsView }));
        ////            break;
        ////    }
        ////}

        /////// <summary>
        /////// Switches the row sort order.
        /////// </summary>
        /////// <param name="sourceView">The source view.</param>
        /////// <param name="targetViews">The target views.</param>
        ////private void SwitchRowSortOrder(MatrixCanvasView sourceView, IEnumerable targetViews)
        ////{
        ////    foreach (MatrixCanvasView targetView in targetViews)
        ////    {
        ////        switch (this.RowSortType)
        ////        {
        ////            case SortType.SortBySums:
        ////                targetView.CustomRowSortOrder = sourceView.IndicesOfSortedRowSumsAscending;
        ////                break;
        ////            case SortType.Custom:
        ////                targetView.CustomRowSortOrder = sourceView.CustomRowSortOrder;
        ////                break;
        ////        }

        ////        targetView.RowSortType = SortType.Custom;
        ////        targetView.RowSortDirection = SortDirection.Ascending;
        ////    }

        ////    sourceView.RowSortType = this.RowSortType;
        ////    sourceView.RowSortDirection = SortDirection.Ascending;
        ////}
        
        /////// <summary>
        /////// Sets the column sort order.
        /////// </summary>
        ////private void SetColumnSortOrder()
        ////{
        ////    var allViews = new[] { SkillsForQuestionsView, IsCorrectView, GuessPosteriorMeansView };

        ////    switch (SortColumnsByView)
        ////    {
        ////        case SortColumnsByView.None:
        ////            foreach (MatrixCanvasView view in allViews)
        ////            {
        ////                view.ColumnSortType = SortType.None;
        ////                view.ColumnSortDirection = SortDirection.Ascending;
        ////            }

        ////            break;
        ////        case SortColumnsByView.SkillsForQuestions:
        ////            this.SwitchColumnSortOrder(SkillsForQuestionsView, allViews.Except(new[] { SkillsForQuestionsView }));
        ////            break;
        ////        case SortColumnsByView.Responses:
        ////            this.SwitchColumnSortOrder(IsCorrectView, allViews.Except(new[] { IsCorrectView }));
        ////            break;
        ////        case SortColumnsByView.GuessPosteriorMeans:
        ////            this.SwitchColumnSortOrder(GuessPosteriorMeansView, allViews.Except(new[] { GuessPosteriorMeansView }));
        ////            break;
        ////    }
        ////}

        /////// <summary>
        /////// Switches the column sort order.
        /////// </summary>
        /////// <param name="sourceView">The source view.</param>
        /////// <param name="targetViews">The target views.</param>
        ////private void SwitchColumnSortOrder(MatrixCanvasView sourceView, IEnumerable targetViews)
        ////{
        ////    foreach (MatrixCanvasView targetView in targetViews)
        ////    {
        ////        targetView.CustomColumnSortOrder = sourceView.CustomColumnSortOrder;
        ////        targetView.ColumnSortType = SortType.Custom;
        ////        targetView.ColumnSortDirection = SortDirection.Ascending;
        ////    }

        ////    sourceView.ColumnSortType = this.ColumnSortType;
        ////    sourceView.ColumnSortDirection = SortDirection.Ascending;
        ////}
    }
}