// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ML.Probabilistic.Distributions;

    /// <summary>
    /// The marginals. Priors and Posteriors are subclasses of this.
    /// </summary>
    public class Marginals
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Marginals"/> class. 
        /// </summary>
        public Marginals()
        {
            this.Skills = new Dictionary<string, Gaussian>();
            this.DrawMargin = Gaussian.PointMass(0);
        }

        /// <summary>
        /// Gets or sets the skills.
        /// </summary>
        public Dictionary<string, Gaussian> Skills { get; set; }

        /// <summary>
        /// Gets or sets the draw margin.
        /// </summary>
        public Gaussian DrawMargin { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Join(", ", this.Skills.Select(ia => string.Format("{0}: {1}", ia.Key, ia.Value)));
        }
    }

    /// <summary>
    /// The priors.
    /// </summary>
    public class Priors : Marginals
    {
    }

    /// <summary>
    /// The posteriors.
    /// </summary>
    public class Posteriors : Marginals
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Posteriors"/> class.
        /// </summary>
        public Posteriors()
        {
            // Default constuctor for the purposes of serialization only.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Posteriors" /> class.
        /// </summary>
        /// <param name="players">The players.</param>
        /// <param name="skills">The skill.</param>
        public Posteriors(IEnumerable<string> players, IEnumerable<Gaussian> skills) : this()
        {
            foreach (var kvp in players.Zip(skills, (k, v) => new { k, v }))
            {
                this.Skills.Add(kvp.k, kvp.v);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Posteriors" /> class.
        /// </summary>
        /// <param name="players">The players.</param>
        /// <param name="skills">The skill.</param>
        /// <param name="drawMargin">The draw margin.</param>
        public Posteriors(IEnumerable<string> players, IEnumerable<Gaussian> skills, Gaussian drawMargin)
            : this()
        {
            foreach (var kvp in players.Zip(skills, (k, v) => new { k, v }))
            {
                this.Skills.Add(kvp.k, kvp.v);
            }

            this.DrawMargin = drawMargin;
        }
    }
}