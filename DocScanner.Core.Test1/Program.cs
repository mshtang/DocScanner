using DocScanner.Core.Services;
using OpenCvSharp;

namespace DocScanner.Core.Test
{
    class Program
    {
        //private static Mat guassianRes = new Mat();
        //private static Mat cannyRes = new Mat();

        static void Main(string[] args)
        {
            TestParams();
        }
        static void TestParams()
        {
            string imagePath = @"..\..\..\..\Data\test1.jpeg";
            using var img = Cv2.ImRead(imagePath, ImreadModes.AnyColor);
            var imageProc = new ImageProcessor(img.Size());

            //Cv2.Resize(src, src, new Size(300, (int)((float)300 / src.Width * src.Height)));
            //Cv2.Resize(src, src, new Size(src.Width / 2, src.Height / 2));
            //Cv2.Resize(dst, dst, src.Size());

            //Cv2.GaussianBlur(dst, dst, new Size(21, 21), 0);
            //Cv2.MedianBlur(dst, dst, 21);
            //Cv2.ImShow("blur", dst);

            //Cv2.Canny(dst, dst, 1, 255, 5);
            //Cv2.ImShow("canny", dst);

            //var lines = Cv2.HoughLines(dst, 1.0, Math.PI / 180, 100);
            //foreach (var line in lines)
            //{
            //    var a = Math.Cos(line.Theta);
            //    var b = Math.Sin(line.Theta);

            //    var x0 = a * line.Rho;
            //    var y0 = b * line.Rho;

            //    var x1 = (int)(x0 + 1000 * (-b));
            //    var y1 = (int)(y0 + 1000 * (a));
            //    var x2 = (int)(x0 - 1000 * (-b));
            //    var y2 = (int)(y0 - 1000 * (a));
            //    Cv2.Line(src, new Point(x1, y1), new Point(x2, y2), Scalar.Yellow, 1);
            //}
            //Cv2.NamedWindow("edges", WindowMode.AutoSize);
            //Cv2.ImShow("edges", src);

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

            //guassianRes.Dispose();
            //cannyRes.Dispose();
            //win.Dispose();

            var (tl, tr, br, bl) = imageProc.FindCorners(img.Clone());

            Cv2.Circle(img, tl, 15, Scalar.Red, 2); //upper-left
            Cv2.Circle(img, tr, 15, Scalar.Yellow, 2); //upper-right
            Cv2.Circle(img, br, 15, Scalar.Blue, 2); //lower-right
            Cv2.Circle(img, bl, 15, Scalar.White, 2); //lower-left

            Cv2.NamedWindow("corners", WindowMode.Normal);
            Cv2.ImShow("corners", img);

            using var res = imageProc.ReprojectImage(img.Clone(), tl, tr, br, bl);
            Cv2.NamedWindow("res", WindowMode.Normal);
            Cv2.ImShow("res", res);

            using var noShadow = ImageProcessor.RemoveShadow(img.Clone());
            Cv2.NamedWindow("noshadow", WindowMode.Normal);
            Cv2.ImShow("noshadow", noShadow);

            while (true)
            {
                if (27 == (char)Cv2.WaitKey()) break;
            }

            Cv2.DestroyAllWindows();
        }
    }
}
