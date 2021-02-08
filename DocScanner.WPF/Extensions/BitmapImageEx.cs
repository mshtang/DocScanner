using System.Windows.Media.Imaging;

namespace DocScanner.WPF.Extenstions
{
    public static class BitmapImageEx
    {
        public static void Save(this BitmapImage image, string filePath)
        {
            BitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create);
            encoder.Save(fileStream);
        }
    }
}
