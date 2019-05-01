// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Models
{
    using global::MeetingYourMatch.Experiments;

    /// <summary>
    /// The random parameters.
    /// </summary>
    public class RandomModelParameters : IModelParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RandomModelParameters"/> class.
        /// </summary>
        public RandomModelParameters()
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether include draws.
        /// </summary>
        public bool IncludeDraws { get; set; }

        /// <summary>
        /// Gets or sets the empirical draw proportion.
        /// </summary>
        public double EmpiricalDrawProportion { get; set; }
    }
}
