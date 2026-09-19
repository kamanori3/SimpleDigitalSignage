using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using KS_Signage.ViewModels;

namespace KS_Signage.Views;

public partial class AdminWindow : Window
{
  private readonly AdminViewModel _viewModel;

  /// <summary>
  /// 表示モード復帰やアプリ終了でウィンドウを閉じるとき true。
  /// × 閉じの未保存確認を出さない。
  /// </summary>
  internal bool AllowClose { get; set; }

  public AdminWindow(AdminViewModel viewModel)
  {
    InitializeComponent();
    _viewModel = viewModel;
    DataContext = viewModel;
    PreviewKeyDown += OnPreviewKeyDown;
    Closing += OnClosing;
    _viewModel.ExitApplicationRequested += OnConfirmedCloseRequested;
    _viewModel.ExitAndPowerOffRequested += OnConfirmedCloseRequested;
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

  public event Action? ExitAndPowerOffRequested
  {
    add => _viewModel.ExitAndPowerOffRequested += value;
    remove => _viewModel.ExitAndPowerOffRequested -= value;
  }

  private void OnConfirmedCloseRequested()
  {
    AllowClose = true;
  }

  private void OnClosing(object? sender, CancelEventArgs e)
  {
    if (AllowClose)
    {
      return;
    }

    if (!_viewModel.TryConfirmLeaveAdmin())
    {
      e.Cancel = true;
    }
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
