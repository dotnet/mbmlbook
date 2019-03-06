// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Linq;
using MBMLCommon;
using Microsoft.ML.Probabilistic.Distributions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static MBMLViews.Extensions;

namespace MBMLViews.Tests
{
    [TestClass]
    public class ExtensionsTests
    {

        /// <summary>
        /// The standard gaussian.
        /// </summary>
        private static Gaussian standardGaussian = new Gaussian(0, 1);

        /// <summary>
        /// Tests the ERF.
        /// </summary>
        [TestMethod]
        public void TestErf()
        {
            const double Epsilon = 1e-6;

            Assert.AreEqual(Erf(0), 0, Epsilon);
            Assert.AreEqual(Erf(0.05), 0.056372, Epsilon);
            Assert.AreEqual(Erf(0.1), 0.1124629, Epsilon);
            Assert.AreEqual(Erf(0.15), 0.167996, Epsilon);
            Assert.AreEqual(Erf(0.2), 0.2227026, Epsilon);
            Assert.AreEqual(Erf(0.25), 0.2763264, Epsilon);
            Assert.AreEqual(Erf(0.3), 0.3286268, Epsilon);
            Assert.AreEqual(Erf(0.35), 0.3793821, Epsilon);
            Assert.AreEqual(Erf(0.4), 0.4283924, Epsilon);
            Assert.AreEqual(Erf(0.45), 0.4754817, Epsilon);
            Assert.AreEqual(Erf(0.5), 0.5204999, Epsilon);
            Assert.AreEqual(Erf(0.55), 0.5633234, Epsilon);
            Assert.AreEqual(Erf(0.6), 0.6038561, Epsilon);
            Assert.AreEqual(Erf(0.65), 0.6420293, Epsilon);
            Assert.AreEqual(Erf(0.7), 0.6778012, Epsilon);
            Assert.AreEqual(Erf(0.75), 0.7111556, Epsilon);
            Assert.AreEqual(Erf(0.8), 0.742101, Epsilon);
            Assert.AreEqual(Erf(0.85), 0.7706681, Epsilon);
            Assert.AreEqual(Erf(0.9), 0.7969082, Epsilon);
            Assert.AreEqual(Erf(0.95), 0.8208908, Epsilon);
            Assert.AreEqual(Erf(1), 0.8427008, Epsilon);
            Assert.AreEqual(Erf(1.1), 0.8802051, Epsilon);
            Assert.AreEqual(Erf(1.2), 0.910314, Epsilon);
        }

        /// <summary>
        /// Tests the normal CDF.
        /// </summary>
        [TestMethod]
        public void TestNormalCDF()
        {
            const double Epsilon = 1e-5;

            Assert.AreEqual(standardGaussian.CumulativeDistributionFunction(double.NegativeInfinity), 0.0);
            Assert.AreEqual(standardGaussian.CumulativeDistributionFunction(double.MinValue), 0.0);
            Assert.AreEqual(standardGaussian.CumulativeDistributionFunction(0), 0.5, Epsilon);
            Assert.AreEqual(standardGaussian.CumulativeDistributionFunction(double.MaxValue), 1.0);
            Assert.AreEqual(standardGaussian.CumulativeDistributionFunction(double.PositiveInfinity), 1.0);
            Assert.AreEqual(standardGaussian.CumulativeDistributionFunction(1), 1 - standardGaussian.CumulativeDistributionFunction(-1), Epsilon);
            Assert.AreEqual(standardGaussian.CumulativeDistributionFunction(1) - standardGaussian.CumulativeDistributionFunction(-1), 0.682689492137, Epsilon);
            Assert.AreEqual(standardGaussian.CumulativeDistributionFunction(2) - standardGaussian.CumulativeDistributionFunction(-2), 0.954499736104, Epsilon);
            Assert.AreEqual(standardGaussian.CumulativeDistributionFunction(3) - standardGaussian.CumulativeDistributionFunction(-3), 0.997300203937, Epsilon);
            Assert.AreEqual(standardGaussian.CumulativeDistributionFunction(4) - standardGaussian.CumulativeDistributionFunction(-4), 0.999936657516, Epsilon);
            Assert.AreEqual(standardGaussian.CumulativeDistributionFunction(5) - standardGaussian.CumulativeDistributionFunction(-5), 0.999999426697, Epsilon);
            Assert.AreEqual(standardGaussian.CumulativeDistributionFunction(6) - standardGaussian.CumulativeDistributionFunction(-6), 0.999999998027, Epsilon);
        }

        /// <summary>
        /// Tests the normal inverse CDF.
        /// </summary>
        [TestMethod]
        public void TestNormalInverseCDF()
        {
            Assert.AreEqual(standardGaussian.InverseCumulativeDistributionFunction(0.5, 10001), 0.0, 1e-3);
            Assert.AreEqual(standardGaussian.InverseCumulativeDistributionFunction(0.975, 10001), 1.959964, 1e-3);
            Assert.AreEqual(standardGaussian.InverseCumulativeDistributionFunction(0.975, 100001), 1.959964, 1e-4);
            Assert.AreEqual(standardGaussian.InverseCumulativeDistributionFunction(0.975, 1000001), 1.959964, 1e-5);
        }

        /// <summary>
        /// Tests the integrate extension.
        /// </summary>
        [TestMethod]
        public void TestIntegrate()
        {
            const double Epsilon = 1e-5;

            Func<double, double> f = x => Math.Pow(x, 9);

            Assert.AreEqual(f.Integrate(0.0, 10.0, 100000), 1000000000.0, Epsilon);

            f = x => 5 + (5 * Math.Sin(2 * Math.PI * x));

            Assert.AreEqual(f.Integrate(0.5, 3.5, 100), 15, Epsilon);

            Point[] points = Enumerable.Range(0, 101).Select(ia => (ia * 0.03) + 0.5).Select(x => new Point(x, f(x))).ToArray();

            Assert.AreEqual(points.Integrate(), 15, Epsilon);
        }
    }
}
