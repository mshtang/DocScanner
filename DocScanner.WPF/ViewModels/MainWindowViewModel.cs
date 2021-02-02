using DocScanner.Core.Services;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = OpenCvSharp.Point;

namespace DocScanner.WPF.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private double _canvasWidth { get; set; }
        private double _canvasHeight { get; set; }
        private int _originalImageWidth;
        private int _originalImageHeight;
        private Point _tl;
        private Point _tr;
        private Point _br;
        private Point _bl;

        private string _imagePath;
        public string ImagePath
        {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }

        private BitmapImage _originalImage;
        public BitmapImage OriginalImage
        {
            get => _originalImage;
            set => SetProperty(ref _originalImage, value);
        }

        private BitmapImage _finalImage;
        public BitmapImage FinalImage
        {
            get => _finalImage;
            set => SetProperty(ref _finalImage, value);
        }

        private PointCollection _boundary;
        public PointCollection Boundary
        {
            get => _boundary;
            set => SetProperty(ref _boundary, value);
        }

        private PointPositionViewModel _topLeft;
        public PointPositionViewModel TopLeft
        {
            get => _topLeft;
            set => SetProperty(ref _topLeft, value);
        }

        private PointPositionViewModel _topRight;
        public PointPositionViewModel TopRight
        {
            get => _topRight;
            set => SetProperty(ref _topRight, value);
        }

        private PointPositionViewModel _bottomLeft;
        public PointPositionViewModel BottomLeft
        {
            get => _bottomLeft;
            set => SetProperty(ref _bottomLeft, value);
        }

        private PointPositionViewModel _bottomRight;
        public PointPositionViewModel BottomRight
        {
            get => _bottomRight;
            set => SetProperty(ref _bottomRight, value);
        }

        public DelegateCommand SelectImage { get; private set; }

        public MainWindowViewModel()
        {
            SelectImage = new DelegateCommand(ExecuteSelectImage);
        }

        private void ExecuteSelectImage()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg)|*.png;*.jpeg|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ImagePath = openFileDialog.FileName;
                OriginalImage = new BitmapImage(new Uri(ImagePath));
                _originalImageWidth = OriginalImage.PixelWidth;
                _originalImageHeight = OriginalImage.PixelHeight;
                ConverteImage();
            }
        }

        private void ConverteImage()
        {
            using var image = Cv2.ImRead(ImagePath, ImreadModes.AnyColor);
            var processor = new ImageProcessor(image.Size());
            (_tl, _tr, _br, _bl) = processor.FindCorners(image);
            UpdateBoundary();

            using var newImage = processor.ReprojectImage(image, _tl, _tr, _br, _bl);
            using var finalImageMat = ImageProcessor.RemoveShadow(newImage);
            FinalImage = BitmapToImageSource(finalImageMat.ToBitmap());
        }

        internal void UpdateCanvasSize(double actualWidth, double actualHeight)
        {
            _canvasWidth = actualWidth;
            _canvasHeight = actualHeight;

            if (Boundary != null)
            {
                UpdateBoundary();
            }
        }

        private void UpdateBoundary()
        {
            var useCanvasWidth = _originalImageWidth / _originalImageHeight > _canvasWidth / _canvasHeight;
            var scale = useCanvasWidth ? _originalImageWidth / _canvasWidth : _originalImageHeight / _canvasHeight;
            Point translation = new Point();
            if (useCanvasWidth)
            {
                translation.X = 0;
                translation.Y = (int)((_canvasHeight - _originalImageHeight / scale) / 2);
            }
            else
            {
                translation.X = (int)((_canvasWidth - _originalImageWidth / scale) / 2);
                translation.Y = 0;
            }

            TopLeft = new PointPositionViewModel(translation + _tl * (1 / scale));
            TopRight = new PointPositionViewModel(translation + _tr * (1 / scale));
            BottomRight = new PointPositionViewModel(translation + _br * (1 / scale));
            BottomLeft = new PointPositionViewModel(translation + _bl * (1 / scale));

            Boundary = new PointCollection
            {
                new System.Windows.Point(TopLeft.X, TopLeft.Y),
                new System.Windows.Point(TopRight.X, TopRight.Y),
                new System.Windows.Point(BottomRight.X, BottomRight.Y),
                new System.Windows.Point(BottomLeft.X, BottomLeft.Y)
            };
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using MemoryStream memory = new MemoryStream();
            bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
            memory.Position = 0;
            BitmapImage bitmapimage = new BitmapImage();
            bitmapimage.BeginInit();
            bitmapimage.StreamSource = memory;
            bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapimage.EndInit();

            return bitmapimage;
        }
    }
}