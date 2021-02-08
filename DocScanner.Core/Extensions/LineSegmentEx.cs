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

        public static bool IsSimilarTo(this LineSegmentPolar line, LineSegmentPolar thatLine)
        {
            return Math.Abs(line.Rho - thatLine.Rho) < 10 && Math.Abs(line.Theta - thatLine.Theta) < 0.1;
        }

        public static LineSegmentPoint Clamp(this LineSegmentPoint line, int width, int height)
        {
            line.P1.X = Math.Clamp(line.P1.X, 0, width);
            line.P1.Y = Math.Clamp(line.P1.Y, 0, height);
            line.P2.X = Math.Clamp(line.P2.X, 0, width);
            line.P2.Y = Math.Clamp(line.P2.Y, 0, height);
            return line;
        }
    }
}
