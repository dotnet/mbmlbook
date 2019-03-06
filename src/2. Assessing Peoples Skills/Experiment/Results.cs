// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AssessingPeoplesSkills
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using MBMLViews;

    using Microsoft.ML.Probabilistic.Distributions;

    /// <summary>
    /// Class to hold inference results
    /// </summary>
    public class Results
    {
        /// <summary>
        /// Gets or sets the skills posteriors.
        /// </summary>
        /// <value>
        /// The skills posteriors.
        /// </value>
        public Bernoulli[][] SkillsPosteriors { get; set; }

        /// <summary>
        /// Gets or sets the guess posteriors.
        /// </summary>
        /// <value>
        /// The guess posteriors.
        /// </value>
        public IList<Beta> GuessPosteriors { get; set; }
        
        /// <summary>
        /// Gets the guess posteriors as dictionary.
        /// </summary>
        /// <value>
        /// The guess posteriors as dictionary.
        /// </value>
        public Dictionary<string, object> GuessPosteriorsAsDictionary
        {
            get
            {
                if (this.GuessPosteriors == null)
                {
                    return null;
                }

                return new Dictionary<string, object>
                    {
                        {
                            "Question",
                            Enumerable.Range(1, this.GuessPosteriors.Count)
                                        .Select(ia => string.Format("{0}", ia))
                                        .ToArray()
                        },
                        { "Posterior", this.GuessPosteriors }
                    };
            }
        }
        
        /// <summary>
        /// Gets or sets the is correct posteriors.
        /// </summary>
        /// <value>
        /// The is correct posteriors.
        /// </value>
        public Bernoulli[][] IsCorrectPosteriors { get; set; }

        /// <summary>
        /// Gets the skills posterior means.
        /// </summary>
        /// <value>
        /// The skills posterior means.
        /// </value>
        public double[][] SkillsPosteriorMeans
        {
            get
            {
                return this.SkillsPosteriors == null ? null : this.SkillsPosteriors.GetMeans();
            }
        }

        /// <summary>
        /// Gets the guess posterior means.
        /// </summary>
        /// <value>
        /// The guess posterior means.
        /// </value>
        public double[] GuessPosteriorMeans
        {
            get
            {
                return this.GuessPosteriors == null ? null : this.GuessPosteriors.GetMeans();
            }
        }

#if NETFULL
        /// <summary>
        /// Gets the guess posterior means and quartiles.
        /// </summary>
        public Microsoft.Research.Glo.Views.PointWithBounds[] GuessPosteriorMeansAndQuartiles
        {
            get
            {
                Func<Beta, double, double> quantileFunc = (beta, d) =>
                    {
                        const double Steps = 1000;
                        double cdf = 0.0;
                        for (int i = 0; i < Steps; i++)
                        {
                            double x = i / Steps;
                            double pdf = Math.Exp(beta.GetLogProb(x));
                            cdf += pdf / Steps;
                            if (cdf > d)
                            {
                                return x;
                            }
                        }

                        return 1.0;
                    };

                return this.GuessPosteriors == null
                           ? null
                           : (from ia in this.GuessPosteriors.Select((beta, index) => new { beta, index })
                              select
                                  new Microsoft.Research.Glo.Views.PointWithBounds(
                                  ia.index + 1,
                                  ia.beta.GetMean(),
                                  quantileFunc(ia.beta, 0.25),
                                  quantileFunc(ia.beta, 0.75))).ToArray();
            }
        }
#endif

        /// <summary>
        /// Gets the is correct posterior means.
        /// </summary>
        /// <value>
        /// The is correct posterior means.
        /// </value>
        public double[][] IsCorrectPosteriorMeans
        {
            get
            {
                return this.IsCorrectPosteriors == null ? null : this.IsCorrectPosteriors.GetMeans();
            }
        }

        /// <summary>
        /// Gets the guess posterior means binned.
        /// </summary>
        /// <value>
        /// The guess posterior means binned.
        /// </value>
        public int[] GuessPosteriorMeansBinned
        {
            get
            {
                return this.GuessPosteriorMeans == null ? null : this.GuessPosteriorMeans.Bin(10, 0.0, 1.0);
            }
        }
    }
}
