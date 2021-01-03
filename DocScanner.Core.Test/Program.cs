using DocScanner.Core.Services;
using OpenCvSharp;
using System;

namespace DocScanner.Core.Test
{
    class Program
    {
        private static Mat guassianRes = new Mat();
        private static Mat cannyRes = new Mat();

        static void Main(string[] args)
        {
            TestParams();
        }
        static void TestParams()
        {
            string imagePath = @"..\..\..\..\Data\test2.jpeg";
            var imageProc = new ImageProcessor(imagePath);

            using var src = imageProc.OrigImage;
            using var dst = imageProc.GrayImage;

            Cv2.Resize(src, src, new Size(300, (int)((float)300 / src.Width * src.Height)));
            Cv2.Resize(dst, dst, src.Size());

            Cv2.GaussianBlur(dst, dst, new Size(21, 21), 0);
            Cv2.MedianBlur(dst, dst, 21);
            // Cv2.AdaptiveThreshold(dst, dst, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 21, 2);
            // skeleton

            Cv2.ImShow("thresh", dst);
            Cv2.Canny(dst, dst, 1, 255, 5);
            //Cv2.Dilate(dst, dst, new Mat(15, 15, MatType.CV_8UC1));

            Cv2.ImShow("canny", dst);
            var lines = Cv2.HoughLines(dst, 1.0, Math.PI / 180, 100);
            foreach (var line in lines)
            {
                var a = Math.Cos(line.Theta);
                var b = Math.Sin(line.Theta);

                var x0 = a * line.Rho;
                var y0 = b * line.Rho;

                var x1 = (int)(x0 + 1000 * (-b));
                var y1 = (int)(y0 + 1000 * (a));
                var x2 = (int)(x0 - 1000 * (-b));
                var y2 = (int)(y0 - 1000 * (a));

                Cv2.Line(src, new Point(x1, y1), new Point(x2, y2), Scalar.Yellow, 1);
            }
            //var lines = Cv2.HoughLinesP(dst, 1.0, Math.PI / 180, 10, 100, 10);

            //foreach (var line in lines)
            //{
            //    Cv2.Line(src, line.P1, line.P2, Scalar.Yellow, 1);
            //}

            Cv2.ImShow("res", src);

            //src.CopyTo(guassianRes);


            //var win = new Window("edges", WindowMode.FreeRatio);
            //var trackbar2 = win.CreateTrackbar("gaussian", 10, 10, ks =>
            //{
            //    Cv2.GaussianBlur(src, guassianRes, new Size(ks * 2 + 3, ks * 2 + 3), 0.0);
            //    win.Image = guassianRes;
            //}); // gausian size(23, 23)

            //var trackbar = win.CreateTrackbar("canny", 75, 256, thr =>
            //{
            //    Cv2.Canny(guassianRes, cannyRes, thr, thr * 2);
            //    win.Image = cannyRes;
            //}); // canny 15

            imageProc.GetOuterBoundary();

            while (true)
            {
                if (27 == (char)Cv2.WaitKey()) break;
            }

            guassianRes.Dispose();
            cannyRes.Dispose();
            //win.Dispose();
        }
    }
}
