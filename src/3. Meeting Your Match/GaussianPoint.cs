// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch
{
    using System;

    using Microsoft.Research.Glo.Views;

    using Microsoft.ML.Probabilistic.Distributions;

    /// <summary>
    /// The Gaussian point. A PointWithBounds that is based on a Gaussian with an "uncertainty" function (e.g. GetVariance) 
    /// </summary>
    public class GaussianPoint : PointWithBounds
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GaussianPoint"/> class.
        /// </summary>
        public GaussianPoint()
        {
            // Parameterless constructor for serialization purposes
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GaussianPoint" /> class.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="gaussian">The gaussian.</param>
        /// <param name="uncertainFunc">The uncertain function.</param>
        public GaussianPoint(double x, Gaussian gaussian, Func<Gaussian, double> uncertainFunc)
            : base(x, gaussian.GetMean(), uncertainFunc(gaussian))
        {
            this.Gaussian = gaussian;
            this.Deviation = uncertainFunc(gaussian);
        }

        /// <summary>
        /// Gets or sets the gaussian.
        /// </summary>
        public Gaussian Gaussian { get; set; }

        /// <summary>
        /// Gets or sets the deviation.
        /// </summary>
        public double Deviation { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0} ({1})", this.Y.ToString("N"), this.Upper.ToString("N"));
        }
    }
}