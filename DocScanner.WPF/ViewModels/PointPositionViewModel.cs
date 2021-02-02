using OpenCvSharp;
using Prism.Mvvm;

namespace DocScanner.WPF.ViewModels
{
    public class PointPositionViewModel : BindableBase
    {
        private Point originalPoint;

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

        public PointPositionViewModel(Point point, double resizeScale, double xOffset, double yOffset)
        {
            originalPoint = point;

            CalculatePosition(resizeScale, xOffset, yOffset);
        }

        public void CalculatePosition(double resizeScale, double xOffset, double yOffset)
        {
            X = originalPoint.X / resizeScale + xOffset;
            Y = originalPoint.Y / resizeScale + yOffset;
        }
    }
}
