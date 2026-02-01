// App.xaml.cs
using System.Windows;
using VideoDetailTransfer.Media;
using VideoDetailTransfer.Persistence;
using VideoDetailTransfer.Ui.ViewModels;
using VideoDetailTransfer.Ui.Views;

namespace VideoDetailTransfer.Ui;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        IProjectStore store = new JsonProjectStore(); // your implementation in App.Persistence

        MainViewModel viewModel = new MainViewModel(store);

        MainWindow window = new MainWindow
        {
            DataContext = viewModel
        };
        window.Show();
    }
}
