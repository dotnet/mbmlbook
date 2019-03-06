using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MBMLCommon
{
    public struct Point
    {
        public Point(double x, double y)
        {
            X = x; Y = y;
        }

        public double Y { get; set; }
        public double X { get; set; }
    }
}
