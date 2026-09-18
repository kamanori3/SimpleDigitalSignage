using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using KS_Signage.Services;
using KS_Signage.ViewModels;

namespace KS_Signage.Views;

/// <summary>
/// コンテンツ表示モード（全画面表示）の code-behind。
/// <para>
/// 責務は View 寄り:
/// コンテンツ表示の枠の ON/OFF、管理モード切替、日次スケジュールの購読、
/// <see cref="MediaElement"/> の再生制御。
/// スライド進行そのものは <see cref="MainViewModel"/> が持つ。
/// </para>
/// </summary>
public partial class MainWindow : Window
{
  private readonly MainViewModel _viewModel;
  private readonly KioskModeService _kioskMode = new();
  private ScheduleService? _scheduleService;
  private AdminWindow? _adminWindow;

  /// <summary>true のあいだはコンテンツ表示を隠し、管理画面を前面にする。</summary>
  private bool _isAdminMode;

  /// <summary>
  /// コンテンツ表示モードに戻るために管理画面を閉じるとき true。
  /// × で閉じたとき（アプリ終了）と区別するために使う。
  /// </summary>
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

  /// <summary>
  /// 初回表示時にコンテンツ表示モードを有効化し、日次スケジュール監視を開始する。
  /// </summary>
  private void OnLoaded(object sender, RoutedEventArgs e)
  {
    _kioskMode.Activate(this);
    Focus();
    Keyboard.Focus(this);

    var context = ApplicationContext.Current!;
    _scheduleService = new ScheduleService(context);
    _scheduleService.AppExitRequested += OnScheduledAppExit;
    _scheduleService.PcShutdownRequested += OnScheduledPcShutdown;
    // 日付跨ぎで再生期限を再評価するため、次スライド境界でのリロードを予約する
    _scheduleService.DateRolledOver += OnDateRolledOver;
    _scheduleService.Start();

    context.Logger.Info("コンテンツ表示モードを有効化しました。");
  }

  /// <summary>ウィンドウ終了時にスケジュール・動画・管理画面・ViewModel を解放する。</summary>
  private void OnClosed(object? sender, EventArgs e)
  {
    if (_scheduleService is not null)
    {
      _scheduleService.AppExitRequested -= OnScheduledAppExit;
      _scheduleService.PcShutdownRequested -= OnScheduledPcShutdown;
      _scheduleService.DateRolledOver -= OnDateRolledOver;
      _scheduleService.Dispose();
      _scheduleService = null;
    }

    _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    StopVideoPlayback();
    _adminWindow?.Close();
    _viewModel.Dispose();
    _kioskMode.Dispose();
  }

  /// <summary>Ctrl+Shift+M で管理モードとコンテンツ表示モードを切り替える。</summary>
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

  /// <summary>
  /// コンテンツ表示を隠し、管理画面を開く。
  /// 入場のたびにファイル名の指定誤り・壊れたファイルがあれば MessageBox を出す（ADR 0009 / ADR 0011）。
  /// </summary>
  private void EnterAdminMode()
  {
    if (_isAdminMode)
    {
      return;
    }

    _kioskMode.Deactivate(this);
    Hide();

    var context = ApplicationContext.Current!;
    // 保存時は ViewModel 経由で実行中のスライドショーへ設定を反映する
    var adminViewModel = new AdminViewModel(context, _ => _viewModel.ApplySettings());
    _adminWindow = new AdminWindow(adminViewModel);
    _adminWindow.ReturnToKioskRequested += ExitAdminMode;
    _adminWindow.ExitApplicationRequested += ExitApplication;
    _adminWindow.Closed += OnAdminWindowClosed;
    _adminWindow.Show();

    _isAdminMode = true;
    ApplicationContext.Current?.Logger.Info("管理モードに切り替えました（Ctrl+Shift+M）。");
    ShowAdminContentAlerts(_adminWindow, context);
  }

  /// <summary>
  /// 実行中の監視フォルダを再スキャンし、ファイル名の指定誤りと壊れたファイルを入場のたびに知らせる。
  /// コンテンツ表示中には出さない。既読管理はせず、開くたびに再表示する。
  /// </summary>
  private static void ShowAdminContentAlerts(Window owner, ApplicationContext context)
  {
    var files = ContentFolderScanner.Scan(context.ResolvedWatchFolder);
    ShowFilenameSuffixErrors(owner, context, files);
    ShowBrokenFileAlerts(owner, context, files);
  }

  private static void ShowFilenameSuffixErrors(
    Window owner,
    ApplicationContext context,
    IReadOnlyList<string> files)
  {
    var message = DisplayDurationParser.FormatIssuesMessage(files);
    if (message is null)
    {
      return;
    }

    context.Logger.Warn("管理モード: ファイル名の指定誤りを検出しました。");
    MessageBox.Show(
      owner,
      message,
      "表示期限の指定",
      MessageBoxButton.OK,
      MessageBoxImage.Warning);
  }

  private static void ShowBrokenFileAlerts(
    Window owner,
    ApplicationContext context,
    IReadOnlyList<string> files)
  {
    var message = ContentFileProbe.FormatBrokenFilesMessage(files);
    if (message is null)
    {
      return;
    }

    context.Logger.Warn("管理モード: 壊れたファイルを検出しました。");
    MessageBox.Show(
      owner,
      message,
      "壊れたファイル",
      MessageBoxButton.OK,
      MessageBoxImage.Warning);
  }

  /// <summary>
  /// 管理画面を閉じてコンテンツ表示モードに戻る。
  /// × 閉じによる終了と区別するため、先に <see cref="_adminClosingForKioskReturn"/> を立てる。
  /// </summary>
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
    _viewModel.RefreshLicenseBanner();
    ApplicationContext.Current?.Logger.Info("コンテンツ表示モードに戻りました（Ctrl+Shift+M）。");
  }

  /// <summary>
  /// 管理画面の × で閉じたとき。コンテンツ表示モードへの復帰ではなくアプリ終了とする。
  /// </summary>
  private void OnAdminWindowClosed(object? sender, EventArgs e)
  {
    // ExitAdminMode からの Close ではここを通しても終了しない
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

  /// <summary>管理画面の「アプリを終了」から呼ばれる。</summary>
  private void ExitApplication()
  {
    _isAdminMode = false;
    ApplicationContext.Current?.Logger.Info("アプリを終了します（管理モード）。");
    Application.Current.Shutdown();
  }

  /// <summary>
  /// 日付が変わったとき。期限切れコンテンツを外すため、次スライド境界で再構築を予約する。
  /// </summary>
  private void OnDateRolledOver()
  {
    ApplicationContext.Current?.RefreshLicense();
    _viewModel.RefreshLicenseBanner();
    _viewModel.RequestPlaylistReload();
  }

  private void OnScheduledAppExit()
  {
    ApplicationContext.Current?.Logger.Info("スケジュールによりアプリを終了します。");
    Application.Current.Shutdown();
  }

  private void OnScheduledPcShutdown()
  {
    var logger = ApplicationContext.Current?.Logger;
    if (logger is not null)
    {
      PcShutdownService.TryShutdown(logger);
    }
  }

  /// <summary>
  /// ViewModel の動画状態に合わせて MediaElement を操作する。
  /// URI 変更だけでは再生が始まらないため、明示的に Play する。
  /// </summary>
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

  /// <summary>動画の再生完了。次スライドへ進める。</summary>
  private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
  {
    _viewModel.OnVideoEnded();
  }

  /// <summary>
  /// 動画の再生失敗。ログを残し、ループが止まらないよう次スライドへ進める。
  /// </summary>
  private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
  {
    var logger = ApplicationContext.Current?.Logger;
    if (e.ErrorException is not null)
    {
      logger?.Error("動画再生失敗", e.ErrorException);
    }
    else
    {
      logger?.Error("動画再生失敗: 不明なエラー");
    }

    // MediaFailed でも OnVideoEnded と同じ経路を通す（壊れた動画で永久停止しない）
    _viewModel.OnVideoEnded();
  }

  private void StopVideoPlayback()
  {
    VideoPlayer.Stop();
    VideoPlayer.Close();
  }
}
