using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NetSecLab.App.ViewModels;

namespace NetSecLab.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(InputElement.PointerWheelChangedEvent, IgnoreClosedComboBoxMouseWheel, RoutingStrategies.Tunnel);
        Closed += (_, _) => DisposeViewModel();
    }

    private static void IgnoreClosedComboBoxMouseWheel(object? sender, PointerWheelEventArgs e)
    {
        if (e.Source is ComboBox comboBox && !comboBox.IsDropDownOpen)
        {
            e.Handled = true;
        }
    }

    private void DisposeViewModel()
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
