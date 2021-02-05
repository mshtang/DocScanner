using DocScanner.Core.Services;
using DocScanner.WPF.Extenstions;
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

        private PointViewModel _topLeft;
        public PointViewModel TopLeft
        {
            get => _topLeft;
            set => SetProperty(ref _topLeft, value);
        }

        private PointViewModel _topRight;
        public PointViewModel TopRight
        {
            get => _topRight;
            set => SetProperty(ref _topRight, value);
        }

        private PointViewModel _bottomLeft;
        public PointViewModel BottomLeft
        {
            get => _bottomLeft;
            set => SetProperty(ref _bottomLeft, value);
        }

        private PointViewModel _bottomRight;
        public PointViewModel BottomRight
        {
            get => _bottomRight;
            set => SetProperty(ref _bottomRight, value);
        }

        private bool _imageLoaded;
        public bool ImageLoaded
        {
            get => _imageLoaded;
            set => SetProperty(ref _imageLoaded, value);
        }

        public DelegateCommand SelectImage { get; private set; }
        public DelegateCommand RegenImage { get; private set; }
        public DelegateCommand SaveImage { get; private set; }

        public MainWindowViewModel()
        {
            SelectImage = new DelegateCommand(ExecuteSelectImage);
            RegenImage = new DelegateCommand(ExecuteRegenImage);
            SaveImage = new DelegateCommand(ExecuteSaveImage);
        }

        private void ExecuteSaveImage()
        {
            var saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                FinalImage.Save(saveFileDialog.FileName);
            }
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

            TransformPoints();

            using var newImage = processor.ReprojectImage(image, _tl, _tr, _br, _bl);
            using var finalImageMat = ImageProcessor.RemoveShadow(newImage);
            FinalImage = BitmapToImageSource(finalImageMat.ToBitmap());
            ImageLoaded = true;
        }

        internal void UpdateCanvasSize(double actualWidth, double actualHeight)
        {
            _canvasWidth = actualWidth;
            _canvasHeight = actualHeight;

            if (Boundary != null)
            {
                TransformPoints();
            }
        }

        private void ExecuteRegenImage()
        {
            using var image = Cv2.ImRead(ImagePath, ImreadModes.AnyColor);
            var processor = new ImageProcessor(image.Size());
            TransformPoints(false);
            using var newImage = processor.ReprojectImage(image, _tl, _tr, _br, _bl);
            using var finalImageMat = ImageProcessor.RemoveShadow(newImage);
            FinalImage = BitmapToImageSource(finalImageMat.ToBitmap());
            ImageLoaded = true;
        }

        /// <summary>
        /// Rescales the boundary when image loads or window size changes
        /// </summary>
        /// <param name="fromImageToCanvas">if true, transform the points on image coordinates to canvas coordinates;
        /// otherwise, from canvas coordinates to image coordinates</param>
        private void TransformPoints(bool fromImageToCanvas = true)
        {
            var useCanvasWidth = _originalImageWidth / _canvasWidth > _originalImageHeight / _canvasHeight;
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

            if (fromImageToCanvas)
            {
                TopLeft = new PointViewModel(this, translation + _tl * (1 / scale));
                TopRight = new PointViewModel(this, translation + _tr * (1 / scale));
                BottomRight = new PointViewModel(this, translation + _br * (1 / scale));
                BottomLeft = new PointViewModel(this, translation + _bl * (1 / scale));

                RedrawBoundary();
            }
            else
            {
                _tl = (TopLeft.ToPoint() - translation) * scale;
                _tr = (TopRight.ToPoint() - translation) * scale;
                _br = (BottomRight.ToPoint() - translation) * scale;
                _bl = (BottomLeft.ToPoint() - translation) * scale;
            }
        }

        private void TransformPointsFromCanvasToImage()
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
        }

        /// <summary>
        /// Redraw the boundary when user moves the corner points
        /// </summary>
        public void RedrawBoundary()
        {
            if (TopLeft != null && TopRight != null && BottomRight != null && BottomLeft != null)
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