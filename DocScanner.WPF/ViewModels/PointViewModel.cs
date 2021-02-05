using OpenCvSharp;
using Prism.Mvvm;

namespace DocScanner.WPF.ViewModels
{
    public class PointViewModel : BindableBase
    {
        private MainWindowViewModel _mainWindowViewModel;

        private double _x;
        public double X
        {
            get => _x;
            set
            {
                SetProperty(ref _x, value);
                _mainWindowViewModel.RedrawBoundary();
            }
        }

        private double _y;
        public double Y
        {
            get => _y;
            set
            {
                SetProperty(ref _y, value);
                _mainWindowViewModel.RedrawBoundary();
            }
        }

        public PointViewModel(MainWindowViewModel mainWindowViewModel, Point point)
        {
            _mainWindowViewModel = mainWindowViewModel;
            X = point.X;
            Y = point.Y;
        }

        public Point ToPoint()
        {
            return new Point(X, Y);
        }
    }
}
