using DocScanner.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace DocScanner.WPF.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OriginalImageCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var canvas = sender as Canvas;

            (DataContext as MainWindowViewModel).UpdateCanvasSize(canvas.ActualWidth, canvas.ActualHeight);
        }

        private void Ellipse_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.Source is Shape shape)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    Point p = e.GetPosition(OriginalImageCanvas);
                    Canvas.SetLeft(shape, p.X - shape.ActualWidth / 2);
                    Canvas.SetTop(shape, p.Y - shape.ActualHeight / 2);
                    shape.CaptureMouse();
                }
                else
                {
                    shape.ReleaseMouseCapture();
                }
            }
        }
    }
}
