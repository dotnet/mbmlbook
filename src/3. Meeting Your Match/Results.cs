// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch
{
    using System.Linq;

    /// <summary>
    /// The results. Holds the posteriors and provides some public properties.
    /// </summary>
    public class Results
    {
        /// <summary>
        /// Gets or sets the posteriors.
        /// </summary>
        public Marginals Posteriors { get; set; }

        /// <summary>
        /// Gets the posterior skill means.
        /// </summary>
        public double[] PosteriorSkillMeans
        {
            get
            {
                return Posteriors?.Skills.Select(ia => ia.Value.GetMean()).ToArray();
            }
        }

        /// <summary>
        /// Gets the posterior skill variances.
        /// </summary>
        public double[] PosteriorSkillVariances
        {
            get
            {
                return Posteriors?.Skills.Select(ia => ia.Value.GetVariance()).ToArray();
            }
        }

        /// <summary>
        /// Gets the conservative skill estimates.
        /// </summary>
        public double[] ConservativeSkillEstimates
        {
            get
            {
                return Posteriors?.Skills.Select(ia => Utils.ConservativeSkill(ia.Value)).ToArray();
            }
        }
    }
}
