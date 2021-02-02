using DocScanner.Core.Extensions;
using OpenCvSharp;
using System;
using System.Collections.Generic;

namespace DocScanner.Core.Services
{
    public class ImageProcessor
    {
        public (Point, Point, Point, Point) Corners { get; set; }

        // Downscale the image such that the width is of 300px
        // This will improve the processing speed in finding the edges
        private const int _smallerWidth = 300;
        private readonly int _smallerHeight;
        private readonly float _scale;

        public ImageProcessor(Size origSize)
        {
            _scale = (float)_smallerWidth / origSize.Width;
            _smallerHeight = (int)(origSize.Height * _scale);
        }

        /// <summary>
        /// Find the 4 corners of a scanned doc 
        /// </summary>
        /// <returns>The corner point in the upper-left, upper-right, lower-right and lower-left position</returns>
        public (Point, Point, Point, Point) FindCorners(Mat img)
        {
            Cv2.CvtColor(img, img, ColorConversionCodes.BGR2GRAY);

            using var gray = new Mat();
            Cv2.Resize(img, gray, new Size(_smallerWidth, _smallerHeight));

            Cv2.GaussianBlur(gray, gray, new Size(21, 21), 0);
            Cv2.MedianBlur(gray, gray, 21);
            Cv2.Canny(gray, gray, 1, 255, 5);
            var lineSegmentPolars = Cv2.HoughLines(gray, 1.0, Math.PI / 180, 100);

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

            var leftLine = new LineSegmentPolar();
            var rightLine = new LineSegmentPolar();
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

            var corners = new List<Point>() {
                topLine.LineIntersection(leftLine).Value * (1 / _scale),
                topLine.LineIntersection(rightLine).Value * (1 / _scale),
                bottomLine.LineIntersection(leftLine).Value * (1 / _scale),
                bottomLine.LineIntersection(rightLine).Value * (1 / _scale)
            };

            return corners.SortCorners();
        }

        // Assume mat can be a 3-channel image
        public static Mat RemoveShadow(Mat mat)
        {
            if (mat.Channels() == 3)
            {
                var planes = Cv2.Split(mat);
                var shadowRemoved = new Mat[3];
                var i = 0;
                foreach (var plane in planes)
                {
                    shadowRemoved[i++] = RemoveShadowSingleChannel(plane);
                }
                var res = new Mat();
                Cv2.Merge(shadowRemoved, res);
                return res;
            }
            else
            {
                return RemoveShadowSingleChannel(mat);
            }
        }

        // mat is a single channel image
        private static Mat RemoveShadowSingleChannel(Mat mat)
        {
            var mask = new Mat();
            Cv2.Dilate(mat, mask, Mat.Ones(3, 3, MatType.CV_8UC1));
            Cv2.MedianBlur(mask, mask, 15);
            var res = new Mat();
            Cv2.Absdiff(mat, mask, res);
            mask.Dispose();
            res = 255 - res;
            Cv2.Normalize(res, res, 0, 255, NormTypes.MinMax, MatType.CV_8UC1);
            return res;
        }

        /// <summary>
        /// Reporject image such that the doc will appear as closely to a rectangle as possible
        /// </summary>
        /// <param name="tl"></param>
        /// <param name="tr"></param>
        /// <param name="br"></param>
        /// <param name="bl"></param>
        /// <returns></returns>
        public Mat ReprojectImage(Mat img, Point tl, Point tr, Point br, Point bl)
        {
            var widthTop = L2Norm(tl, tr);
            var widthBottom = L2Norm(bl, br);
            var newWidth = widthTop > widthBottom ? (int)widthTop : (int)widthBottom;

            var heightLeft = L2Norm(tl, bl);
            var heightRight = L2Norm(tr, br);
            var newHeight = heightLeft > heightRight ? (int)heightLeft : (int)heightRight;

            var srcPoints = new List<Point2f> { tl, tr, br, bl };
            var dstPoints = new List<Point2f>
            {
                new Point2f(0, 0),
                new Point2f(newWidth - 1, 0),
                new Point2f(newWidth - 1, newHeight - 1),
                new Point2f(0, newHeight - 1)
            };
            using var transform = Cv2.GetPerspectiveTransform(srcPoints, dstPoints);

            var correctedMat = new Mat();
            Cv2.WarpPerspective(img, correctedMat, transform, new Size(newWidth, newHeight));

            return correctedMat;
        }

        private static double L2Norm(Point pt1, Point pt2)
        {
            return Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y));
        }
    }
}
