using System.Windows;
using VideoDetailTransfer.Ui.ViewModels;
using VideoDetailTransfer.Ui.Views;

namespace VideoDetailTransfer.Ui;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        MainViewModel vm = new MainViewModel();
        MainWindow window = new MainWindow
        {
            DataContext = vm
        };

        window.Show();
    }
}