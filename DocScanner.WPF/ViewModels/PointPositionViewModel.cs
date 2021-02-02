using OpenCvSharp;
using Prism.Mvvm;

namespace DocScanner.WPF.ViewModels
{
    public class PointPositionViewModel : BindableBase
    {
        private double _x;
        public double X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }

        private double _y;
        public double Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }

        public PointPositionViewModel(Point point)
        {
            X = point.X;
            Y = point.Y;
        }
    }
}
