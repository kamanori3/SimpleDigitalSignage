using System.Reflection;
using System.Windows.Input;
using Microsoft.Win32;
using PdfSignage.Models;
using PdfSignage.Services;

namespace PdfSignage.ViewModels;

/// <summary>
/// 管理画面の ViewModel。設定の編集・保存・実行中アプリへの反映を行う。
/// </summary>
public sealed class AdminViewModel : ViewModelBase
{
  private readonly ApplicationContext _context;
  private readonly Action<AppSettings> _applySettings;

  private string _watchFolderPath = "";
  private string _defaultDisplaySecondsText = "";
  private bool _windowsAutoStart;
  private bool _appExitTimeEnabled;
  private string _appExitTime = "";
  private bool _pcShutdownTimeEnabled;
  private string _pcShutdownTime = "";
  private string _recoveryMessage = "";
  private string _statusMessage = "";
  private bool _hasValidationError;

  public AdminViewModel(ApplicationContext context, Action<AppSettings> applySettings)
  {
    _context = context;
    _applySettings = applySettings;

    LoadFromSettings(context.Settings);

    BrowseWatchFolderCommand = new RelayCommand(BrowseWatchFolder);
    SaveAndApplyCommand = new RelayCommand(SaveAndApply);
    ReturnToKioskCommand = new RelayCommand(() => ReturnToKioskRequested?.Invoke());
    ExitApplicationCommand = new RelayCommand(() => ExitApplicationRequested?.Invoke());
  }

  public event Action? ReturnToKioskRequested;
  public event Action? ExitApplicationRequested;

  public string VersionText { get; } =
    Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

  public string WatchFolderPath
  {
    get => _watchFolderPath;
    set => SetProperty(ref _watchFolderPath, value);
  }

  public string DefaultDisplaySecondsText
  {
    get => _defaultDisplaySecondsText;
    set => SetProperty(ref _defaultDisplaySecondsText, value);
  }

  public bool WindowsAutoStart
  {
    get => _windowsAutoStart;
    set => SetProperty(ref _windowsAutoStart, value);
  }

  public bool AppExitTimeEnabled
  {
    get => _appExitTimeEnabled;
    set => SetProperty(ref _appExitTimeEnabled, value);
  }

  public string AppExitTime
  {
    get => _appExitTime;
    set => SetProperty(ref _appExitTime, value);
  }

  public bool PcShutdownTimeEnabled
  {
    get => _pcShutdownTimeEnabled;
    set => SetProperty(ref _pcShutdownTimeEnabled, value);
  }

  public string PcShutdownTime
  {
    get => _pcShutdownTime;
    set => SetProperty(ref _pcShutdownTime, value);
  }

  public string RecoveryMessage
  {
    get => _recoveryMessage;
    set => SetProperty(ref _recoveryMessage, value);
  }

  public string StatusMessage
  {
    get => _statusMessage;
    private set
    {
      if (SetProperty(ref _statusMessage, value))
      {
        OnPropertyChanged(nameof(HasStatusMessage));
      }
    }
  }

  public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

  public bool HasValidationError
  {
    get => _hasValidationError;
    private set => SetProperty(ref _hasValidationError, value);
  }

  public ICommand BrowseWatchFolderCommand { get; }
  public ICommand SaveAndApplyCommand { get; }
  public ICommand ReturnToKioskCommand { get; }
  public ICommand ExitApplicationCommand { get; }

  private void LoadFromSettings(AppSettings settings)
  {
    WatchFolderPath = settings.WatchFolderPath;
    DefaultDisplaySecondsText = settings.DefaultDisplaySeconds.ToString();
    WindowsAutoStart = settings.WindowsAutoStart;
    AppExitTimeEnabled = !string.IsNullOrWhiteSpace(settings.AppExitTime);
    AppExitTime = settings.AppExitTime ?? "18:00";
    PcShutdownTimeEnabled = !string.IsNullOrWhiteSpace(settings.PcShutdownTime);
    PcShutdownTime = settings.PcShutdownTime ?? "22:00";
    RecoveryMessage = settings.RecoveryMessage;
    StatusMessage = "";
    HasValidationError = false;
  }

  private void BrowseWatchFolder()
  {
    var dialog = new OpenFolderDialog
    {
      Title = "監視フォルダを選択",
      InitialDirectory = Directory.Exists(WatchFolderPath)
        ? WatchFolderPath
        : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
    };

    if (dialog.ShowDialog() == true)
    {
      WatchFolderPath = dialog.FolderName;
    }
  }

  private void SaveAndApply()
  {
    if (!int.TryParse(DefaultDisplaySecondsText.Trim(), out var defaultDisplaySeconds))
    {
      HasValidationError = true;
      StatusMessage = "デフォルト表示秒数は整数で入力してください。";
      return;
    }

    if (!SettingsValidator.TryValidate(
          WatchFolderPath.Trim(),
          defaultDisplaySeconds,
          AppExitTimeEnabled,
          AppExitTime,
          PcShutdownTimeEnabled,
          PcShutdownTime,
          RecoveryMessage,
          out var errorMessage))
    {
      HasValidationError = true;
      StatusMessage = errorMessage;
      return;
    }

    HasValidationError = false;

    var settings = new AppSettings
    {
      WatchFolderPath = WatchFolderPath.Trim(),
      DefaultDisplaySeconds = defaultDisplaySeconds,
      WindowsAutoStart = WindowsAutoStart,
      AppExitTime = AppExitTimeEnabled
        ? SettingsValidator.NormalizeTime(AppExitTime)
        : null,
      PcShutdownTime = PcShutdownTimeEnabled
        ? SettingsValidator.NormalizeTime(PcShutdownTime)
        : null,
      RecoveryMessage = RecoveryMessage.Trim()
    };

    _context.SettingsService.Save(settings);
    _context.ApplySettings(settings);
    WindowsAutoStartService.Apply(settings.WindowsAutoStart, _context.Logger);
    _applySettings(settings);

    StatusMessage = "設定を保存し、実行中のアプリへ反映しました。";
    _context.Logger.Info("管理画面から設定を保存・反映しました。");
  }
}
