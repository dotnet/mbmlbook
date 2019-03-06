// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MBMLViews.Tests
{
    [TestClass]
    class RealRangeTests
    {
        /// <summary>
        /// Tests the rounding method.
        /// </summary>
        [TestMethod]
        public void TestRound()
        {
            Action<RealRange, RealRange> compare = (i, t) =>
            {
                RealRange r = i.Round();
                Assert.IsTrue(r.Equals(t), "Inputs: {0}\nExpected: {1}\nActual: {2}", i, t, r);
            };

            const double D1 = 0.0081564574540389997;
            const double D2 = 0.997722;

            compare(new RealRange { Min = D1, Max = D2 }, new RealRange { Min = 0.0, Max = 1.0 });
            compare(new RealRange { Min = D1, Max = D2 * 10 }, new RealRange { Min = 0.0, Max = 10.0 });
            compare(new RealRange { Min = D1 + 9, Max = D2 + 9 }, new RealRange { Min = 9.0, Max = 10.0 });
            compare(new RealRange { Min = D1 - 0.5, Max = D2 - 0.5 }, new RealRange { Min = -0.5, Max = 0.5 });
            compare(new RealRange { Min = -D2, Max = -D1 }, new RealRange { Min = -1.0, Max = 0.0 });
        }
    }
}
