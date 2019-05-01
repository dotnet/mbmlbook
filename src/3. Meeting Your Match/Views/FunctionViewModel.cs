// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLViews.Views
{
    using System;
    using System.Linq;
#if NETFULL
    using Point = System.Windows.Point;
#else
    using Point = MBMLCommon.Point;
#endif
    /// <summary>
    /// The function view model.
    /// </summary>
    public class FunctionViewModel
    {
        /// <summary>
        /// The points.
        /// </summary>
        private Point[] points;

        /// <summary>
        /// The function.
        /// </summary>
        private Func<double, double> function;

        /// <summary>
        /// Gets or sets the range.
        /// </summary>
        public RealRange Range { get; set; }

        /// <summary>
        /// Gets or sets the function.
        /// </summary>
        public Func<double, double> Function
        {
            get
            {
                return this.function;
            }

            set
            {
                this.function = value;
                this.points = this.GetPoints();
            }
        }

        /// <summary>
        /// Gets or sets the points.
        /// </summary>
        public Point[] Points
        {
            get
            {
                return this.points ?? (this.points = this.GetPoints());
            }

            set
            {
                this.points = value;
            }
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the points.
        /// </summary>
        /// <returns>The points.</returns>
        private Point[] GetPoints()
        {
            if (this.Range == null || this.Function == null)
            {
                return null;
            }

            return this.Range.Values.Select(x => new Point(x, this.Function(x))).ToArray();
        }
    }
}