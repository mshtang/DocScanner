using OpenCvSharp;
using System;

namespace DocScanner.Core.Extensions
{
    public static class LineSegmentEx
    {
        public static bool IsBetween(this LineSegmentPolar line, double first, double second)
        {
            return line.Theta >= first / 180 * Math.PI && line.Theta <= second / 180 * Math.PI;
        }
    }
}
