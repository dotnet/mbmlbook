// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLViews.Views
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using MBMLViews;

    /// <summary>
    /// The matrix canvas view model.
    /// </summary>
    public class MatrixCanvasViewModel
    {
        /// <summary>
        /// The row sort container.
        /// </summary>
        private readonly SortContainer rowSortContainer;

        /// <summary>
        /// The column sort container.
        /// </summary>
        private readonly SortContainer columnSortContainer;

        /// <summary>
        /// The data range.
        /// </summary>
        private RealRange dataRange = new RealRange(); //// { Min = 0.0, Max = 1.0 };

        /// <summary>
        /// Initializes a new instance of the <see cref="MatrixCanvasViewModel"/> class.
        /// </summary>
        public MatrixCanvasViewModel()
        {
            this.rowSortContainer = new SortContainer(this);
            this.columnSortContainer = new SortContainer(this);
        }

        /// <summary>
        /// Gets or sets the row sums.
        /// </summary>
        public double[] RowSums { get; set; }

        /// <summary>
        /// Gets or sets the row sums.
        /// </summary>
        public int[] RowLengths { get; set; }

        /// <summary>
        /// Gets or sets the data type.
        /// </summary>
        public TypeCode DataType { get; set; }

        /// <summary>
        /// Gets or sets the rows.
        /// </summary>
        public int Rows { get; set; }

        /// <summary>
        /// Gets or sets the cols. 
        /// The number of "columns" (could be jagged or nullable, so will be max of column lengths)
        /// </summary>
        public int Cols { get; set; }

        /// <summary>
        /// Gets the number of elements.
        /// </summary>
        public int Elements
        {
            get
            {
                return this.Rows * this.Cols;
            }
        }

        /// <summary>
        /// Gets or sets the data range.
        /// </summary>
        public RealRange DataRange
        {
            get
            {
                return this.dataRange;
            }

            set
            {
                this.dataRange = value;
            }
        }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        public Array Data { get; set; }

        /// <summary>
        /// Gets or sets the column sort container.
        /// </summary>
        public SortContainer Sorter { get; set; }

        /// <summary>
        /// Gets or sets the row sorter.
        /// </summary>
        public SortContainer RowSorter { get; set; }

        /// <summary>
        /// Gets the row enumerator.
        /// </summary>
        /// <returns>
        /// The row enumerator
        /// </returns>
        internal IEnumerable<int> GetRowEnumerator()
        {
            IEnumerable<int> enumerator = Enumerable.Range(0, this.Rows);

            switch (this.rowSortContainer.SortType)
            {
                case SortType.SortBySums:
                    enumerator = ArrayHelpers.GetIndicesFromRowSums(this.RowSums);
                    if (this.rowSortContainer.SortDirection == SortDirection.Descending)
                    {
                        enumerator = enumerator.Reverse();
                    }

                    break;
                case SortType.Custom:
                    enumerator = this.rowSortContainer.CustomSortOrder;
                    break;
            }

            return enumerator;
        }

        /// <summary>
        /// Gets the column enumerator.
        /// </summary>
        /// <returns>The column enumerator</returns>
        internal IEnumerable GetColumnEnumerator()
        {
            IEnumerable<int> enumerator = Enumerable.Range(0, this.Cols);

            if (this.columnSortContainer.SortType == SortType.Custom)
            {
                enumerator = this.columnSortContainer.CustomSortOrder;
            }

            return enumerator;
        }

        public int GetRowLength(int i)
        {
            if (RowLengths == null) return Cols;
            return RowLengths[i];
        }
    }
}