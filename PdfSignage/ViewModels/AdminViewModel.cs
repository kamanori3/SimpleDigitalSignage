using System.Reflection;
using System.Windows;
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
  private AppSettings _savedSettings = new();

  private string _watchFolderPath = "";
  private string _googleDriveFolderUrl = "";
  private string _googleDriveApiKey = "";
  private string _googleDriveSyncIntervalMinutesText = "";
  private string _defaultDisplaySecondsText = "";
  private bool _windowsAutoStart;
  private bool _appExitTimeEnabled;
  private string _appExitTime = "";
  private bool _pcShutdownTimeEnabled;
  private string _pcShutdownTime = "";
  private string _recoveryMessage = "";
  private string _statusMessage = "";
  private bool _hasValidationError;
  private bool _pcShutdownUsesDefault = true;
  private bool _isApplyingDefaultPcShutdown;

  public AdminViewModel(ApplicationContext context, Action<AppSettings> applySettings)
  {
    _context = context;
    _applySettings = applySettings;

    LoadFromSettings(context.Settings);

    BrowseWatchFolderCommand = new RelayCommand(BrowseWatchFolder);
    SaveAndApplyCommand = new RelayCommand(SaveAndApply);
    ReturnToKioskCommand = new RelayCommand(RequestReturnToKiosk);
    ExitApplicationCommand = new RelayCommand(RequestExitApplication);
  }

  public event Action? ReturnToKioskRequested;
  public event Action? ExitApplicationRequested;

  public string WindowTitle { get; } =
    $"簡易デジタルサイネージ　ver.{FormatVersionLabel()} - 管理モード";

  public string WatchFolderPath
  {
    get => _watchFolderPath;
    set => SetProperty(ref _watchFolderPath, value);
  }

  public string GoogleDriveFolderUrl
  {
    get => _googleDriveFolderUrl;
    set
    {
      if (SetProperty(ref _googleDriveFolderUrl, value))
      {
        OnPropertyChanged(nameof(IsWatchFolderEnabled));
      }
    }
  }

  public string GoogleDriveApiKey
  {
    get => _googleDriveApiKey;
    set => SetProperty(ref _googleDriveApiKey, value);
  }

  public string GoogleDriveSyncIntervalMinutesText
  {
    get => _googleDriveSyncIntervalMinutesText;
    set => SetProperty(ref _googleDriveSyncIntervalMinutesText, value);
  }

  public bool IsWatchFolderEnabled => string.IsNullOrWhiteSpace(GoogleDriveFolderUrl);

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
    set
    {
      if (SetProperty(ref _appExitTimeEnabled, value) && value && _pcShutdownUsesDefault && PcShutdownTimeEnabled)
      {
        ApplyDefaultPcShutdownTime();
      }
    }
  }

  public string AppExitTime
  {
    get => _appExitTime;
    set
    {
      if (SetProperty(ref _appExitTime, value) && _pcShutdownUsesDefault && PcShutdownTimeEnabled)
      {
        ApplyDefaultPcShutdownTime();
      }
    }
  }

  public bool PcShutdownTimeEnabled
  {
    get => _pcShutdownTimeEnabled;
    set
    {
      if (SetProperty(ref _pcShutdownTimeEnabled, value) && value)
      {
        ApplyDefaultPcShutdownTime();
      }
    }
  }

  public string PcShutdownTime
  {
    get => _pcShutdownTime;
    set
    {
      if (SetProperty(ref _pcShutdownTime, value) && !_isApplyingDefaultPcShutdown)
      {
        _pcShutdownUsesDefault = false;
      }
    }
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
    _savedSettings = CloneSettings(settings);
    WatchFolderPath = settings.WatchFolderPath;
    GoogleDriveFolderUrl = settings.GoogleDriveFolderUrl;
    GoogleDriveApiKey = settings.GoogleDriveApiKey;
    GoogleDriveSyncIntervalMinutesText = settings.GoogleDriveSyncIntervalMinutes.ToString();
    DefaultDisplaySecondsText = settings.DefaultDisplaySeconds.ToString();
    WindowsAutoStart = settings.WindowsAutoStart;
    AppExitTimeEnabled = !string.IsNullOrWhiteSpace(settings.AppExitTime);
    AppExitTime = settings.AppExitTime ?? "18:00";
    PcShutdownTimeEnabled = !string.IsNullOrWhiteSpace(settings.PcShutdownTime);

    if (settings.PcShutdownTime is not null)
    {
      _isApplyingDefaultPcShutdown = true;
      PcShutdownTime = settings.PcShutdownTime;
      _isApplyingDefaultPcShutdown = false;
      _pcShutdownUsesDefault = false;
    }
    else
    {
      ApplyDefaultPcShutdownTime();
    }

    RecoveryMessage = settings.RecoveryMessage;
    StatusMessage = "";
    HasValidationError = false;
  }

  private void ApplyDefaultPcShutdownTime()
  {
    if (!SettingsValidator.IsValidTime(AppExitTime))
    {
      return;
    }

    _isApplyingDefaultPcShutdown = true;
    PcShutdownTime = ScheduleTimeHelper.GetDefaultPcShutdownTime(AppExitTime);
    _isApplyingDefaultPcShutdown = false;
    _pcShutdownUsesDefault = true;
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
    TrySaveAndApply();
  }

  private void RequestReturnToKiosk()
  {
    if (!ConfirmUnsavedChangesBeforeProceed())
    {
      return;
    }

    ReturnToKioskRequested?.Invoke();
  }

  private void RequestExitApplication()
  {
    if (!ConfirmUnsavedChangesBeforeProceed())
    {
      return;
    }

    ExitApplicationRequested?.Invoke();
  }

  private bool ConfirmUnsavedChangesBeforeProceed()
  {
    if (!HasUnsavedChanges())
    {
      return true;
    }

    var result = MessageBox.Show(
      "設定が変更されています。保存して適用しますか？",
      "設定の確認",
      MessageBoxButton.YesNoCancel,
      MessageBoxImage.Question);

    return result switch
    {
      MessageBoxResult.Yes => TrySaveAndApply(),
      MessageBoxResult.No => true,
      _ => false
    };
  }

  private bool HasUnsavedChanges()
  {
    if (!TryBuildSettingsFromForm(out var current, requireValid: false))
    {
      return true;
    }

    return !SettingsEquals(current, _savedSettings);
  }

  private bool TrySaveAndApply()
  {
    if (!TryBuildSettingsFromForm(out var settings, requireValid: true))
    {
      return false;
    }

    _context.SettingsService.Save(settings);
    _context.ApplySettings(settings);
    WindowsAutoStartService.Apply(settings.WindowsAutoStart, _context.Logger);
    _applySettings(settings);

    _savedSettings = CloneSettings(settings);
    HasValidationError = false;
    StatusMessage = "設定を保存し、実行中のアプリへ反映しました。";
    _context.Logger.Info("管理画面から設定を保存・反映しました。");
    return true;
  }

  private bool TryBuildSettingsFromForm(out AppSettings settings, bool requireValid)
  {
    settings = new AppSettings();

    if (!int.TryParse(DefaultDisplaySecondsText.Trim(), out var defaultDisplaySeconds))
    {
      if (requireValid)
      {
        HasValidationError = true;
        StatusMessage = "デフォルト表示秒数は整数で入力してください。";
      }

      return false;
    }

    if (!int.TryParse(GoogleDriveSyncIntervalMinutesText.Trim(), out var driveSyncMinutes))
    {
      if (requireValid)
      {
        HasValidationError = true;
        StatusMessage = "Drive の同期間隔は整数（分）で入力してください。";
      }

      return false;
    }

    if (requireValid && !SettingsValidator.TryValidate(
          WatchFolderPath.Trim(),
          defaultDisplaySeconds,
          AppExitTimeEnabled,
          AppExitTime,
          PcShutdownTimeEnabled,
          PcShutdownTime,
          RecoveryMessage,
          GoogleDriveFolderUrl,
          GoogleDriveApiKey,
          driveSyncMinutes,
          out var errorMessage))
    {
      HasValidationError = true;
      StatusMessage = errorMessage;
      return false;
    }

    settings.WatchFolderPath = string.IsNullOrWhiteSpace(WatchFolderPath)
      ? "D:\\Signage"
      : WatchFolderPath.Trim();
    settings.GoogleDriveFolderUrl = GoogleDriveFolderUrl.Trim();
    settings.GoogleDriveApiKey = GoogleDriveApiKey.Trim();
    settings.GoogleDriveSyncIntervalMinutes = driveSyncMinutes;
    settings.DefaultDisplaySeconds = defaultDisplaySeconds;
    settings.WindowsAutoStart = WindowsAutoStart;
    settings.AppExitTime = AppExitTimeEnabled
      ? SettingsValidator.NormalizeTime(AppExitTime)
      : null;
    settings.PcShutdownTime = PcShutdownTimeEnabled
      ? SettingsValidator.NormalizeTime(PcShutdownTime)
      : null;
    settings.RecoveryMessage = RecoveryMessage.Trim();
    return true;
  }

  private static bool SettingsEquals(AppSettings left, AppSettings right)
  {
    return left.WatchFolderPath == right.WatchFolderPath
           && left.GoogleDriveFolderUrl == right.GoogleDriveFolderUrl
           && left.GoogleDriveApiKey == right.GoogleDriveApiKey
           && left.GoogleDriveSyncIntervalMinutes == right.GoogleDriveSyncIntervalMinutes
           && left.DefaultDisplaySeconds == right.DefaultDisplaySeconds
           && left.WindowsAutoStart == right.WindowsAutoStart
           && left.AppExitTime == right.AppExitTime
           && left.PcShutdownTime == right.PcShutdownTime
           && left.RecoveryMessage == right.RecoveryMessage;
  }

  private static AppSettings CloneSettings(AppSettings source)
  {
    return new AppSettings
    {
      WatchFolderPath = source.WatchFolderPath,
      GoogleDriveFolderUrl = source.GoogleDriveFolderUrl,
      GoogleDriveApiKey = source.GoogleDriveApiKey,
      GoogleDriveSyncIntervalMinutes = source.GoogleDriveSyncIntervalMinutes,
      DefaultDisplaySeconds = source.DefaultDisplaySeconds,
      WindowsAutoStart = source.WindowsAutoStart,
      AppExitTime = source.AppExitTime,
      PcShutdownTime = source.PcShutdownTime,
      RecoveryMessage = source.RecoveryMessage
    };
  }

  private static string FormatVersionLabel()
  {
    var version = Assembly.GetExecutingAssembly().GetName().Version;
    if (version is null)
    {
      return "00.0.00";
    }

    var build = version.Build >= 0 ? version.Build : 0;
    return $"{version.Major:D2}.{version.Minor}.{build:D2}";
  }
}
