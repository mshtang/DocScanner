using DocScanner.WPF.Views;
using Prism.Ioc;
using Prism.Unity;
using System.Windows;

namespace DocScanner.WPF
{
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            var window = Container.Resolve<MainWindow>();
            return window;
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {

        }
    }
}