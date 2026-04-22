using System.Windows;
using GpuCatMonitor.Views;

namespace GpuCatMonitor;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var mainWindow = new CatWindow();
        mainWindow.Show();
    }
}
