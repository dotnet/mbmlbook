// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using MBMLViews;

    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Math;

    using MeetingYourMatch.Views;

#if NETFULL
    using Point = System.Windows.Point;
#else
    using Point = MBMLCommon.Point;
#endif
    
    /// <summary>
    /// The demo figures. Some schematic illustrations for the early part of the chapter
    /// </summary>
    public class DemoFigures
    {
        /// <summary>
        /// Gets the Jill skill.
        /// </summary>
        [Browsable(false)]
        public Gaussian Jskill
        {
            get { return new Gaussian(15, 25); }
        }

        /// <summary>
        /// Gets the Fred skill.
        /// </summary>
        [Browsable(false)]
        public Gaussian Fskill
        {
            get { return new Gaussian(12.5, 25); }
        }

        /// <summary>
        /// Gets the jill fred.
        /// </summary>
        public Dictionary<string, Gaussian> JillFred
        {
            get
            {
                return new Dictionary<string, Gaussian>
                           {
                                { "Jskill", new Gaussian(15, 25) },
                                { "Fskill", new Gaussian(12.5, 25) }
                           };
            }
        }

        /// <summary>
        /// Gets the jill fred. Schematic illustration of the performance space in a two player game
        /// </summary>
        public PerformanceSpaceViewModel PerformanceSpace
        {
            get
            {
                return new PerformanceSpaceViewModel(this.Jskill, this.Fskill, 0)
                           {
                               Player1Name = "Jill",
                               Player2Name = "Fred",
                               DrawMargin = double.NaN
                           };
            }
        }

        /// <summary>
        /// Gets the jill fred samples.
        /// </summary>
        public PerformanceSpaceViewModel PerformanceSpaceSamples
        {
            get
            {
                return new PerformanceSpaceViewModel(this.Jskill, this.Fskill, 1000)
                           {
                               Player1Name = "Jill",
                               Player2Name = "Fred",
                               DrawMargin = double.NaN
                           };
            }
        }

        /// <summary>
        /// Gets the performance space with draws.
        /// </summary>
        public PerformanceSpaceViewModel PerformanceSpaceWithDraws
        {
            get
            {
                var jill = new Gaussian(120, 25);
                var fred = new Gaussian(100, 25);

                return new PerformanceSpaceViewModel(jill, fred, 0)
                           {
                               Player1Name = "Jill",
                               Player2Name = "Fred",
                               DrawMargin = 10.0
                           };
            }
        }

        /// <summary>
        /// Gets some example Gaussian distributions.
        /// </summary>
        public Gaussian[] Gaussians
        {
            get
            {
                return new[] { new Gaussian(0, 0.2), new Gaussian(0, 1.0), new Gaussian(0, 5.0), new Gaussian(-2, 0.5) };
            }
        }

        /// <summary>
        /// Gets the performance curve. Illustration of a "bell curve" showing the performance variation
        /// </summary>
        public Gaussian PerformanceCurve
        {
            get
            {
                return this.Jskill;
            }
        }

        /// <summary>
        /// Gets a demo of the Gaussian cumulative distribution function.
        /// </summary>
        public Point[] CumGauss
        {
            get
            {
                Gaussian gaussian = new Gaussian(0, 1);
                const int Samples = 1500;

                IEnumerable<double> x = Enumerable.Range(0, Samples).Select(ia => (ia - (Samples / 2.0)) / Samples * 6.0);
                return x.Select(ia => new Point(ia, gaussian.CumulativeDistributionFunction(ia))).ToArray();
            }
        }

        /// <summary>
        /// Gets the Gaussian cumulative distribution function with shaded Gaussian.
        /// </summary>
        public Point[][] CumGaussWithShaded
        {
            get
            {
                var gaussian = new Gaussian(0, 1);
                const int Samples = 1500;

                var x = Enumerable.Range(0, Samples).Select(ia => (ia - (Samples / 2.0)) / Samples * 6.0).ToArray();
                return new[]
                           {
                               x.Select(ia => new Point(ia, Math.Exp(gaussian.GetLogProb(ia)))).ToArray(),
                               x.Select(ia => new Point(ia, gaussian.CumulativeDistributionFunction(ia))).ToArray()
                           };
            }
        }

        /// <summary>
        /// Gets the sampled performance distributions for an example skill distribution.
        /// </summary>
        public Dictionary<string,object> SampledPerformanceDistributions
        {
            get
            {
                const double Mean = 100;
                const double Variance = 25; //5^2
                const double PerformanceVariance = 25; //5^2
                var skill = new Gaussian(Mean, Variance);

                // Set random seed
                Rand.Restart(2);

                // Sample from the skills, and create a Gaussian around the 
                // sampled skill with the specified performance variance
                var samples = Enumerable.Range(0, 10000).Select(ia => new Gaussian(skill.Sample(), PerformanceVariance)).ToArray();
                var range = Utils.GetRange(samples);

                return
                    new Dictionary<string,object>
                    {
                        { "Samples", samples.Take(3).ToArray() },
                        { "Averaged", new Dictionary<string,object>
                            {
                                { "2", Utils.GaussianAverage(samples.Take(2).ToArray(), range) },
                                { "3", Utils.GaussianAverage(samples.Take(3).ToArray(), range) },
                                { "5", Utils.GaussianAverage(samples.Take(5).ToArray(), range) },
                                { "6", Utils.GaussianAverage(samples.Take(6).ToArray(), range) },
                                { "10", Utils.GaussianAverage(samples.Take(10).ToArray(), range) },
                                { "20", Utils.GaussianAverage(samples.Take(20).ToArray(), range) },
                                { "100", Utils.GaussianAverage(samples.Take(100).ToArray(), range) },
                                { "1000", Utils.GaussianAverage(samples.Take(1000).ToArray(), range) },
                                { "10000", Utils.GaussianAverage(samples, range) }
                            }
                        }
                    };
            }
        }
    }
}
