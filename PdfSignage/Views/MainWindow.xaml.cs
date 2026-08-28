using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using PdfSignage.Services;
using PdfSignage.ViewModels;

namespace PdfSignage.Views;

public partial class MainWindow : Window
{
  private readonly MainViewModel _viewModel;
  private readonly KioskModeService _kioskMode = new();
  private AdminWindow? _adminWindow;
  private bool _isAdminMode;
  private bool _adminClosingForKioskReturn;

  public MainWindow()
  {
    InitializeComponent();
    _viewModel = new MainViewModel(ApplicationContext.Current!);
    DataContext = _viewModel;
    _viewModel.PropertyChanged += OnViewModelPropertyChanged;

    Loaded += OnLoaded;
    Closed += OnClosed;
  }

  private void OnLoaded(object sender, RoutedEventArgs e)
  {
    _kioskMode.Activate(this);
    Focus();
    Keyboard.Focus(this);
    ApplicationContext.Current?.Logger.Info("キオスクモードを有効化しました。");
  }

  private void OnClosed(object? sender, EventArgs e)
  {
    _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    StopVideoPlayback();
    _adminWindow?.Close();
    _viewModel.Dispose();
    _kioskMode.Dispose();
  }

  private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
  {
    if (e.Key == Key.M && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
    {
      ToggleAdminMode();
      e.Handled = true;
    }
  }

  private void ToggleAdminMode()
  {
    if (_isAdminMode)
    {
      ExitAdminMode();
      return;
    }

    EnterAdminMode();
  }

  private void EnterAdminMode()
  {
    if (_isAdminMode)
    {
      return;
    }

    _kioskMode.Deactivate(this);
    Hide();

    var context = ApplicationContext.Current!;
    var adminViewModel = new AdminViewModel(context, _ => _viewModel.ApplySettings());
    _adminWindow = new AdminWindow(adminViewModel);
    _adminWindow.ReturnToKioskRequested += ExitAdminMode;
    _adminWindow.ExitApplicationRequested += ExitApplication;
    _adminWindow.Closed += OnAdminWindowClosed;
    _adminWindow.Show();

    _isAdminMode = true;
    ApplicationContext.Current?.Logger.Info("管理モードに切り替えました（Ctrl+Shift+M）。");
  }

  private void ExitAdminMode()
  {
    if (!_isAdminMode)
    {
      return;
    }

    _isAdminMode = false;
    _adminClosingForKioskReturn = true;

    if (_adminWindow is not null)
    {
      DetachAdminWindowHandlers(_adminWindow);
      _adminWindow.Close();
      _adminWindow = null;
    }

    _adminClosingForKioskReturn = false;

    _kioskMode.Activate(this);
    Show();
    Activate();
    Keyboard.Focus(this);
    ApplicationContext.Current?.Logger.Info("キオスクモードに戻りました（Ctrl+Shift+M）。");
  }

  private void OnAdminWindowClosed(object? sender, EventArgs e)
  {
    if (_adminClosingForKioskReturn || sender is not AdminWindow adminWindow)
    {
      return;
    }

    DetachAdminWindowHandlers(adminWindow);
    if (_adminWindow == adminWindow)
    {
      _adminWindow = null;
    }

    if (!_isAdminMode)
    {
      return;
    }

    _isAdminMode = false;
    ExitApplication();
  }

  private void DetachAdminWindowHandlers(AdminWindow adminWindow)
  {
    adminWindow.ReturnToKioskRequested -= ExitAdminMode;
    adminWindow.ExitApplicationRequested -= ExitApplication;
    adminWindow.Closed -= OnAdminWindowClosed;
  }

  private void ExitApplication()
  {
    _isAdminMode = false;
    ApplicationContext.Current?.Logger.Info("アプリを終了します（管理モード）。");
    Application.Current.Shutdown();
  }

  private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
  {
    if (e.PropertyName == nameof(MainViewModel.VideoSource) && _viewModel.VideoSource is not null)
    {
      VideoPlayer.Stop();
      VideoPlayer.Play();
      return;
    }

    if (e.PropertyName == nameof(MainViewModel.IsVideoVisible) && !_viewModel.IsVideoVisible)
    {
      StopVideoPlayback();
    }
  }

  private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
  {
    _viewModel.OnVideoEnded();
  }

  private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
  {
    ApplicationContext.Current?.Logger.Error(
      $"動画再生失敗: {e.ErrorException?.Message ?? "不明なエラー"}");
    _viewModel.OnVideoEnded();
  }

  private void StopVideoPlayback()
  {
    VideoPlayer.Stop();
    VideoPlayer.Close();
  }
}
