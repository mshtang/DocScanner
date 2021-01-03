using DocScanner.Core.Extensions;
using OpenCvSharp;
using System;
using System.Collections.Generic;

namespace DocScanner.Core.Services
{
    public class ImageProcessor
    {
        public List<Point> Boundary { get; set; }

        public Mat OrigImage { get; set; }

        public Mat GrayImage { get; set; }

        private Mat _grayImage = new Mat();

        // Downscale the image such that the width is of 300px
        // This will improve the processing speed in finding the edges
        private const int _smallerWidth = 300;
        private int _smallerHeight;

        public ImageProcessor()
        {

        }

        public ImageProcessor(string imageFile)
        {
            OrigImage = new Mat(imageFile, ImreadModes.AnyColor);
            Cv2.CvtColor(OrigImage, _grayImage, ColorConversionCodes.BGR2GRAY);
            GrayImage = _grayImage.Clone();
            _smallerHeight = (int)(GrayImage.Height * _smallerWidth / (float)GrayImage.Width);
        }


        public List<Point> GetOuterBoundary()
        {
            using var img = new Mat();

            Cv2.Resize(GrayImage, img, new Size(_smallerWidth, _smallerHeight));
            Cv2.GaussianBlur(img, img, new Size(21, 21), 0);
            Cv2.MedianBlur(img, img, 21);
            Cv2.Canny(img, img, 1, 255, 5);

            var lineSegmentPolars = Cv2.HoughLines(img, 1.0, Math.PI / 180, 100);
            var lines = new List<LineSegmentPoint>();

            var hLines = new List<LineSegmentPolar>();
            var vLines = new List<LineSegmentPolar>();
            foreach (var line in lineSegmentPolars)
            {
                if (line.IsBetween(0, 3) || line.IsBetween(177, 180))
                    hLines.Add(line);
                else if (line.IsBetween(87, 93))
                    vLines.Add(line);
            }

            var topLine = new LineSegmentPolar();
            var bottomLine = new LineSegmentPolar();
            var leftLine = new LineSegmentPolar();
            var rightLine = new LineSegmentPolar();
            if (hLines.Count >= 2)
            {
                topLine = hLines.MinBy(line => line.Rho);
                bottomLine = hLines.MaxBy(line => line.Rho);
            }
            else if (hLines.Count == 1)
            {
                if (hLines[0].Rho > _smallerHeight / 2)
                {
                    topLine = new LineSegmentPolar(0, 0);
                    bottomLine = hLines[0];
                }
                else
                {
                    topLine = hLines[0];
                    bottomLine = new LineSegmentPolar(_smallerHeight, 0);
                }
            }
            else
            {
                topLine = new LineSegmentPolar(0, 0);
                bottomLine = new LineSegmentPolar(_smallerHeight, 0);
            }

            if (vLines.Count >= 2)
            {
                leftLine = vLines.MinBy(line => line.Rho);
                rightLine = vLines.MaxBy(line => line.Rho);
            }
            else if (vLines.Count == 1)
            {
                if (vLines[0].Rho > _smallerWidth / 2)
                {
                    leftLine = new LineSegmentPolar(0, (float)Math.PI / 2);
                    rightLine = vLines[0];
                }
                else
                {
                    leftLine = vLines[0];
                    rightLine = new LineSegmentPolar(_smallerWidth, (float)Math.PI / 2);
                }
            }
            else
            {
                leftLine = new LineSegmentPolar(0, (float)Math.PI / 2);
                rightLine = new LineSegmentPolar(_smallerWidth, (float)Math.PI / 2);
            }

            var upperLeft = topLine.LineIntersection(leftLine).Value;
            var upperRight = topLine.LineIntersection(rightLine).Value;
            var lowerLeft = bottomLine.LineIntersection(leftLine).Value;
            var lowerRight = bottomLine.LineIntersection(rightLine).Value;


#if DEBUG
            Cv2.NamedWindow("lines", WindowMode.Normal);

            //Cv2.Line(OrigImage, topLine.P1, topLine.P2, Scalar.Yellow, 2);
            //Cv2.Line(OrigImage, bottomLine.P1, topLine.P2, Scalar.Yellow, 2);
            //Cv2.Line(OrigImage, leftLine.P1, topLine.P2, Scalar.Yellow, 2);
            //Cv2.Line(OrigImage, rightLine.P1, topLine.P2, Scalar.Yellow, 2);
            var scale = OrigImage.Width / (float)_smallerWidth;
            Cv2.Circle(OrigImage, upperLeft * scale, 15, Scalar.Blue, 2);
            Cv2.Circle(OrigImage, upperRight * scale, 15, Scalar.Blue, 2);
            Cv2.Circle(OrigImage, lowerLeft * scale, 15, Scalar.Blue, 2);
            Cv2.Circle(OrigImage, lowerRight * scale, 15, Scalar.Blue, 2);

            Cv2.ImShow("lines", OrigImage);

            while (true)
            {
                if (27 == Cv2.WaitKey()) break;
            }

            Cv2.DestroyAllWindows();
#endif
            return new List<Point> { upperLeft, upperRight, lowerRight, lowerLeft };
        }

        private LineSegmentPoint PolarToCartisian(double theta, double rho)
        {
            var a = Math.Cos(theta);
            var b = Math.Sin(theta);

            var x0 = a * rho;
            var y0 = b * rho;

            var x1 = (int)(x0 + 1000 * (-b));
            var y1 = (int)(y0 + 1000 * (a));
            var x2 = (int)(x0 - 1000 * (-b));
            var y2 = (int)(y0 - 1000 * (a));

            return new LineSegmentPoint(new Point(x1, y1), new Point(x2, y2));
        }

        private LineSegmentPoint SelectOptimalLine(List<LineSegmentPoint> lines, Position pos)
        {
            if (lines.Count == 0)
            {
                return pos switch
                {
                    Position.left => new LineSegmentPoint(new Point(0, 0), new Point(0, _smallerHeight)),
                    Position.right => new LineSegmentPoint(new Point(_smallerWidth, 0), new Point(_smallerWidth, _smallerHeight)),
                    Position.top => new LineSegmentPoint(new Point(0, 0), new Point(_smallerWidth, 0)),
                    Position.bottom => new LineSegmentPoint(new Point(0, _smallerHeight), new Point(_smallerWidth, _smallerHeight)),
                    _ => new LineSegmentPoint(),
                };
            }
            else if (lines.Count == 1)
                return lines[0];
            else
            {
                return pos switch
                {
                    Position.left => lines.MinBy(line => line.P1.X + line.P2.X),
                    Position.right => lines.MaxBy(line => line.P1.X + line.P2.X),
                    Position.top => lines.MinBy(line => line.P1.Y + line.P2.Y),
                    Position.bottom => lines.MaxBy(line => line.P2.Y + line.P2.Y),
                    _ => new LineSegmentPoint(),
                };
            }
        }

        private enum Position
        {
            left,
            right,
            top,
            bottom
        }
    }
}
