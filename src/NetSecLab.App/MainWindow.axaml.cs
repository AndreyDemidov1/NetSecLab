using Avalonia.Controls;
using NetSecLab.App.ViewModels;

namespace NetSecLab.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closed += (_, _) => DisposeViewModel();
    }

    private void DisposeViewModel()
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
