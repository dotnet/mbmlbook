// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLViews
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    using MBMLViews.Annotations;

    /// <summary>
    /// A class defining a minimum and maximum for a range on the real line
    /// </summary>
    public sealed class RealRange : IEquatable<RealRange>, INotifyPropertyChanged
    {
        /// <summary>
        /// The epsilon
        /// </summary>
        private double epsilon = double.Epsilon;

        /// <summary>
        /// The min.
        /// </summary>
        private double min = double.NaN;

        /// <summary>
        /// The max.
        /// </summary>
        private double max = double.NaN;

        /// <summary>
        /// The step size.
        /// </summary>
        private double stepSize = double.NaN;

        /// <summary>
        /// The steps.
        /// </summary>
        private int steps = 2;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the min.
        /// </summary>
        public double Min
        {
            get
            {
                return this.min;
            }

            set
            {
                if (value > this.max)
                {
                    this.min = this.max;
                    this.max = value;
                }
                else
                {
                    this.min = value;
                }

                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the max.
        /// </summary>
        public double Max
        {
            get
            {
                return this.max;
            }

            set
            {
                if (value < this.min)
                {
                    this.max = this.min;
                    this.min = value;
                }
                else
                {
                    this.max = value;
                }

                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the step size. Note that the step size is rounded to the nearest integer fraction of the range Delta.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Should not be larger than range Delta</exception>
        public double StepSize
        {
            get
            {
                return this.stepSize;
            }

            set
            {
                if (value > this.Delta)
                {
                    throw new ArgumentOutOfRangeException("value", @"Should not be larger than range Delta");
                }

                this.steps = (int)Math.Floor(this.Delta / value);
                this.stepSize = this.Delta / this.steps;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the steps.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Must be greater than 1</exception>
        public int Steps
        {
            get
            {
                return this.steps;
            }

            set
            {
                if (value < 2)
                {
                    throw new ArgumentOutOfRangeException("value", @"Must be greater than 2");
                }

                this.steps = value;
                this.stepSize = this.Delta / (value - 1);
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the delta.
        /// </summary>
        public double Delta
        {
            get
            {
                return this.Max - this.Min;
            }
        }
        
        /// <summary>
        /// Gets the values.
        /// </summary>
        public IEnumerable<double> Values
        {
            get
            {
                for (int i = 0; i < this.steps; i++)
                {
                    double d = this.Min + (i * this.StepSize);
                    yield return d;
                }
            }
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        public int Count
        {
            get
            {
                return this.steps;
            }
        }

        /// <summary>
        /// Gets a value indicating whether is read only.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Expands the range to include the specified value.
        /// </summary>
        /// <param name="d">The d.</param>
        public void Expand(double d)
        {
            this.Min = double.IsNaN(this.Min) ? d : Math.Min(this.Min, d);
            this.Max = double.IsNaN(this.Max) ? d : Math.Max(this.Max, d);
        }

        /// <summary>
        /// Rounds the specified range.
        /// </summary>
        /// <returns>
        /// The rounded <see cref="RealRange" />
        /// </returns>
        /// <exception cref="System.InvalidOperationException">Min or max is invalid</exception>
        public RealRange Round()
        {
            if (!this.IsValid())
            {
                throw new InvalidOperationException("Min or max is invalid");
            }

            double factor = Math.Sign(this.Min) == Math.Sign(this.Max)
                                ? Math.Pow(10, Math.Ceiling(Math.Log10(this.Delta)))
                                : Math.Pow(10, Math.Ceiling(Math.Log10(this.Delta))) / 10;

            RealRange r = new RealRange
                              {
                                  Min = Math.Floor(this.Min / factor) * factor,
                                  Max = Math.Ceiling(this.Max / factor) * factor
                              };
                
            return r;
        }

        /// <summary>
        /// Determines whether this instance is valid.
        /// </summary>
        /// <returns>The <see cref="bool"/></returns>
        public bool IsValid()
        { 
            return !double.IsNaN(this.Min) && !double.IsNaN(this.Max);
        }

        #region IEquatable
        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type (within a specified tolerance).
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(RealRange other, double tolerance)
        {
            this.epsilon = tolerance;
            return this.Equals(other);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(RealRange other)
        {
            return (Math.Abs(this.Min - other.Min) < this.epsilon) && (Math.Abs(this.Max - other.Max) < this.epsilon);
        }
        #endregion IEquatable

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        public void Clear()
        {
            this.Min = double.NaN;
            this.Max = double.NaN;
            this.stepSize = double.NaN;
            this.steps = 2;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("Min {0}, Max {1}", this.Min, this.Max);
        }

        /// <summary>
        /// Fixes the errors.
        /// </summary>
        public void FixErrors()
        {
            if (double.IsNaN(this.Min))
            {
                this.Min = 0.0;
            }

            if (double.IsNaN(this.Max))
            {
                this.Max = 1.0;
            }

            if (Math.Abs(this.Delta) < double.Epsilon)
            {
                this.Max = this.Min + 1;
            }
        }

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}