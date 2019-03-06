// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLViews.Views
{
    using System;
    using System.Collections.Generic;

    #region enums
    /// <summary>
    /// The sort type.
    /// </summary>
    public enum SortType
    {
        /// <summary>
        /// No sorting
        /// </summary>
        None,

        /// <summary>
        /// Sort by row/column sums
        /// </summary>
        SortBySums,

        /// <summary>
        /// Custom sort order
        /// </summary>
        Custom
    }

    /// <summary>
    /// The sort direction.
    /// </summary>
    public enum SortDirection
    {
        /// <summary>
        /// Sort ascending.
        /// </summary>
        Ascending,

        /// <summary>
        /// Sort descending.
        /// </summary>
        Descending
    }
    #endregion

    /// <summary>
    /// The sort container.
    /// </summary>
    public class SortContainer
    {
        #region fields
        /// <summary>
        /// The view model.
        /// </summary>
        private readonly MatrixCanvasViewModel viewModel;

        /// <summary>
        /// The column sort type.
        /// </summary>
        private SortType sortType = SortType.None;

        /// <summary>
        /// The custom sort order.
        /// </summary>
        private IList<int> customSortOrder;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SortContainer" /> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        public SortContainer(MatrixCanvasViewModel viewModel)
        {
            this.SortDirection = SortDirection.Ascending;
            this.viewModel = viewModel;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets or sets the sort type.
        /// </summary>
        /// <exception cref="NullReferenceException">Custom sort order should be set first </exception>
        public SortType SortType
        {
            get
            {
                return this.sortType;
            }

            set
            {
                if ((this.sortType == SortType.Custom) && (this.CustomSortOrder == null))
                {
                    throw new NullReferenceException("CustomSortOrder");
                }

                this.sortType = value;
            }
        }

        /// <summary>
        /// Gets or sets the sort direction.
        /// </summary>
        public SortDirection SortDirection { get; set; }

        /// <summary>
        /// Gets or sets the custom sort order.
        /// </summary>
        public IList<int> CustomSortOrder
        {
            get
            {
                return this.customSortOrder;
            }

            set
            {
                if (ArrayHelpers.CheckCustomSortOrderValidity(value, 0, this.viewModel.Cols))
                {
                    this.customSortOrder = value;
                }
                else
                {
                    throw new ArgumentException("Custom list sort order is invalid");
                }
            }
        }
        #endregion
    }
}