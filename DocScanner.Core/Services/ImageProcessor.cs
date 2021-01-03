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

        public Mat GrayImage { get; set; } = new Mat();

        // Downscale the image such that the width is of 300px
        // This will improve the processing speed in finding the edges
        private const int _smallerWidth = 300;
        private readonly int _smallerHeight;
        private readonly float _scale;

        public ImageProcessor()
        {

        }

        public ImageProcessor(string imageFile)
        {
            OrigImage = new Mat(imageFile, ImreadModes.AnyColor);
            Cv2.CvtColor(OrigImage, GrayImage, ColorConversionCodes.BGR2GRAY);
            _scale = (float)_smallerWidth / OrigImage.Width;
            _smallerHeight = (int)(OrigImage.Height * _scale);
        }

        public List<Point> FindCorners()
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

            return new List<Point> { upperLeft * (1 / _scale), upperRight * (1 / _scale), lowerRight * (1 / _scale), lowerLeft * (1 / _scale) };
        }
    }
}
