using DocScanner.Core.Extensions;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public (Point, Point, Point, Point) FindCorners(in Mat src)
        {
            using var img = src.Clone();
            Cv2.Resize(img, img, new Size(_smallerWidth, _smallerHeight));

            using var gray = new Mat();
            if (img.Channels() == 3)
                Cv2.CvtColor(img, gray, ColorConversionCodes.BGR2GRAY);

            Cv2.GaussianBlur(gray, gray, new Size(21, 21), 0);
            Cv2.MedianBlur(gray, gray, 21);
            Cv2.Canny(gray, gray, 1, 255, 5);

            var candidateLines = new List<LineSegmentPolar>();
            var initThresh = 100;
            while (candidateLines.Count < 4 && initThresh > 0)
            {
                candidateLines.Clear();
#if DEBUG
                using var canvas1 = img.Clone();
                using var canvas2 = img.Clone();
#endif
                var lineSegmentPolars = Cv2.HoughLines(gray, 1.0, Math.PI / 180, initThresh);
                foreach (var line in lineSegmentPolars)
                {
#if DEBUG
                    Console.WriteLine($"Angle: {line.Theta / Math.PI * 180}");
                    var lineSegment = line.ToSegmentPoint(1000);
                    Cv2.Line(canvas1, lineSegment.P1, lineSegment.P2, Scalar.Yellow, 1);
#endif
                    // if the line is far from horizontal or vertical, ignore
                    if (!(line.IsBetween(0, 5) || line.IsBetween(175, 180) || line.IsBetween(85, 95)))
                        continue;

                    // if the line is similar to a candidate line, ignore
                    if (candidateLines.Where(l => l.IsSimilarTo(line)).Count() != 0)
                        continue;

                    candidateLines.Add(line);
#if DEBUG
                    Cv2.Line(canvas2, lineSegment.P1, lineSegment.P2, Scalar.Yellow, 2);
#endif
                }
                initThresh -= 10;
            }

            var hLines = new List<LineSegmentPoint>();
            var vLines = new List<LineSegmentPoint>();
            foreach (var line in candidateLines)
            {
                if (line.IsBetween(0, 5) || line.IsBetween(175, 180))
                {
                    vLines.Add(line.ToSegmentPoint(1000));
                }
                else if (line.IsBetween(85, 95))
                {
                    hLines.Add(line.ToSegmentPoint(1000));
                }
            }

            var topLineMax = 0;
            var bottomLineMin = img.Height;
            var topLine = new LineSegmentPoint();
            var bottomLine = new LineSegmentPoint();
            var vCentralLine = new LineSegmentPoint(new Point(img.Width / 2, 0), new Point(img.Width / 2, img.Height));
#if DEBUG
            using var canvas = img.Clone();
            Cv2.Line(canvas, vCentralLine.P1, vCentralLine.P2, Scalar.Blue, 2);
#endif
            foreach (var hLine in hLines)
            {
                var pt = hLine.LineIntersection(vCentralLine).Value;
                if (pt.Y < img.Height / 2) // search for topline
                {
                    if (pt.Y > topLineMax)
                    {
                        topLineMax = pt.Y;
                        topLine = hLine;
                    }
#if DEBUG
                    Cv2.Line(canvas, topLine.P1, topLine.P2, Scalar.Yellow, 2);
                    Cv2.Circle(canvas, pt, 3, Scalar.Red, 3);
#endif
                }
                else // search for bottomLine
                {
                    if (pt.Y < bottomLineMin)
                    {
                        bottomLineMin = pt.Y;
                        bottomLine = hLine;
                    }
#if DEBUG
                    Cv2.Line(canvas, bottomLine.P1, bottomLine.P2, Scalar.Yellow, 2);
                    Cv2.Circle(canvas, pt, 3, Scalar.Red, 3);
#endif
                }
            }

            var leftLineMax = 0;
            var rightLineMin = img.Width;
            var leftLine = new LineSegmentPoint();
            var rightLine = new LineSegmentPoint();
            var hCentralLine = new LineSegmentPoint(new Point(0, img.Height / 2), new Point(img.Width, img.Height / 2));
#if DEBUG
            Cv2.Line(canvas, hCentralLine.P1, hCentralLine.P2, Scalar.Blue, 2);
#endif

            foreach (var vLine in vLines)
            {
                var pt = vLine.LineIntersection(hCentralLine).Value;
                if (pt.X < img.Width / 2) // search for leftLine
                {
                    if (pt.X > leftLineMax)
                    {
                        leftLineMax = pt.X;
                        leftLine = vLine;
                    }
#if DEBUG
                    Cv2.Line(canvas, leftLine.P1, leftLine.P2, Scalar.Yellow, 2);
                    Cv2.Circle(canvas, pt, 3, Scalar.Red, 3);
#endif
                }
                else // search for rightLine
                {
                    if (pt.X < rightLineMin)
                    {
                        rightLineMin = pt.X;
                        rightLine = vLine;
                    }
#if DEBUG
                    Cv2.Line(canvas, rightLine.P1, rightLine.P2, Scalar.Yellow, 2);
                    Cv2.Circle(canvas, pt, 3, Scalar.Red, 3);
#endif
                }

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
