using DocScanner.Core.Services;
using OpenCvSharp;

namespace DocScanner.Core.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            string imagePath = @"..\..\..\..\Data\test1.jpeg";
            var imageProc = new ImageProcessor(imagePath);

            using var src = imageProc.GrayImage;
            using var dst = new Mat();
            src.CopyTo(dst);

            var win = new Window("edges", WindowMode.FreeRatio, dst);
            var trackbar = win.CreateTrackbar("thresh", 100, 256, thr =>
            {
                Cv2.Canny(src, dst, thr, thr * 2);
                win.Image = dst;
            });

            while (true)
            {
                if (27 == (char)Cv2.WaitKey())
                    break;
            }

            win.Dispose();
        }

    }
}
