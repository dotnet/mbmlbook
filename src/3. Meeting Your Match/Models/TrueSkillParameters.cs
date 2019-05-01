// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Models
{
    using global::MeetingYourMatch.Experiments;

    /// <summary>
    /// The true skill parameters.
    /// </summary>
    public class TrueSkillParameters : IModelParameters
    {
        /// <summary>
        /// Gets or sets the performance variance.
        /// </summary>
        public double PerformanceVariance { get; set; }

        /// <summary>
        /// Gets or sets the dynamics variance.
        /// </summary>
        public double DynamicsVariance { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                "PerformanceVariance: {0}, DynamicsVariance {1}",
                this.PerformanceVariance.ToString("N"),
                this.DynamicsVariance.ToString("N"));
        }
    }
}
