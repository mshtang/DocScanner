using OpenCvSharp;
using System.Collections.Generic;

namespace DocScanner.Core.Services
{
    public class ImageProcessor
    {
        public List<Point> Boundary { get; set; }

        public Mat OrigImage { get; set; }

        public Mat GrayImage { get; set; }

        private Mat _grayImage = new Mat();

        public ImageProcessor()
        {

        }

        public ImageProcessor(string imageFile)
        {
            OrigImage = new Mat(imageFile, ImreadModes.AnyColor);
            Cv2.CvtColor(OrigImage, _grayImage, ColorConversionCodes.BGR2GRAY);
            GrayImage = _grayImage;
        }


        public List<Point> GetOuterBoundary()
        {
            return null;
        }

    }
}
