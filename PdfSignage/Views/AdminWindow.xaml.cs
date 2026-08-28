using System.Windows;
using System.Windows.Input;
using PdfSignage.ViewModels;

namespace PdfSignage.Views;

public partial class AdminWindow : Window
{
  private readonly AdminViewModel _viewModel;

  public AdminWindow(AdminViewModel viewModel)
  {
    InitializeComponent();
    _viewModel = viewModel;
    DataContext = viewModel;
    PreviewKeyDown += OnPreviewKeyDown;
  }

  public event Action? ReturnToKioskRequested
  {
    add => _viewModel.ReturnToKioskRequested += value;
    remove => _viewModel.ReturnToKioskRequested -= value;
  }

  public event Action? ExitApplicationRequested
  {
    add => _viewModel.ExitApplicationRequested += value;
    remove => _viewModel.ExitApplicationRequested -= value;
  }

  private void OnPreviewKeyDown(object sender, KeyEventArgs e)
  {
    if (e.Key == Key.M && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
    {
      _viewModel.ReturnToKioskCommand.Execute(null);
      e.Handled = true;
    }
  }
}
