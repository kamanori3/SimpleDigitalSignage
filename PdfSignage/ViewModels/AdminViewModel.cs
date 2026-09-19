using System.Reflection;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using PdfSignage.Licensing;
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
  private string _defaultDisplaySecondsText = "";
  private bool _windowsAutoStart;
  private bool _appExitTimeEnabled;
  private string _appExitTime = "";
  private string _recoveryMessage = "";
  private string _accessKeyInput = "";
  private string _licenseStatusText = "";
  private string _licenseDetailText = "";
  private string _logDirectory = "";
  private string _statusMessage = "";
  private bool _hasValidationError;

  public AdminViewModel(ApplicationContext context, Action<AppSettings> applySettings)
  {
    _context = context;
    _applySettings = applySettings;

    LoadFromSettings(context.Settings);

    BrowseWatchFolderCommand = new RelayCommand(BrowseWatchFolder);
    SaveAndApplyCommand = new RelayCommand(SaveAndApply);
    ApplyAccessKeyCommand = new RelayCommand(ApplyAccessKey);
    ReturnToKioskCommand = new RelayCommand(RequestReturnToKiosk);
    ExitApplicationCommand = new RelayCommand(RequestExitApplication);
    ExitAndPowerOffCommand = new RelayCommand(RequestExitAndPowerOff);
  }

  public event Action? ReturnToKioskRequested;
  public event Action? ExitApplicationRequested;
  public event Action? ExitAndPowerOffRequested;

  public string WindowTitle { get; } =
    $"{PdfSignage.AppInfo.ProductName}　ver.{FormatVersionLabel()} - 管理モード";

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

  public string RecoveryMessage
  {
    get => _recoveryMessage;
    set => SetProperty(ref _recoveryMessage, value);
  }

  public string AccessKeyInput
  {
    get => _accessKeyInput;
    set => SetProperty(ref _accessKeyInput, value);
  }

  public string LicenseStatusText
  {
    get => _licenseStatusText;
    private set => SetProperty(ref _licenseStatusText, value);
  }

  public string LicenseDetailText
  {
    get => _licenseDetailText;
    private set => SetProperty(ref _licenseDetailText, value);
  }

  public string LogDirectory
  {
    get => _logDirectory;
    private set => SetProperty(ref _logDirectory, value);
  }

  /// <summary>スタンドアロン商品のとき、監視フォルダがこの PC 内に限る旨を出す。</summary>
  public bool ShowLocalWatchFolderHint => !_savedSettings.AllowNetworkWatchFolder;

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
  public ICommand ApplyAccessKeyCommand { get; }
  public ICommand ReturnToKioskCommand { get; }
  public ICommand ExitApplicationCommand { get; }
  public ICommand ExitAndPowerOffCommand { get; }

  private void LoadFromSettings(AppSettings settings)
  {
    _savedSettings = CloneSettings(settings);
    WatchFolderPath = settings.WatchFolderPath;
    DefaultDisplaySecondsText = settings.DefaultDisplaySeconds.ToString();
    WindowsAutoStart = settings.WindowsAutoStart;
    AppExitTimeEnabled = !string.IsNullOrWhiteSpace(settings.AppExitTime);
    AppExitTime = settings.AppExitTime ?? "18:00";
    RecoveryMessage = settings.RecoveryMessage;
    AccessKeyInput = settings.AccessKey;
    StatusMessage = "";
    HasValidationError = false;
    RefreshLicenseUi();
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

  private void ApplyAccessKey()
  {
    var key = AccessKeyInput.Trim();
    if (string.IsNullOrEmpty(key))
    {
      HasValidationError = true;
      StatusMessage = "アクセスキーを貼り付けてください。";
      return;
    }

    var verify = LicenseService.VerifyAccessKey(key, _context.Logger);
    if (!verify.IsValid)
    {
      HasValidationError = true;
      StatusMessage = LicenseService.FormatInvalidKeyMessage();
      return;
    }

    _context.Settings.AccessKey = key;
    _context.SettingsService.Save(_context.Settings);
    _savedSettings.AccessKey = key;
    AccessKeyInput = key;
    _context.RefreshLicense();
    RefreshLicenseUi();
    HasValidationError = false;
    StatusMessage = "アクセスキーを適用しました。";
    _context.Logger.Info("管理画面からアクセスキーを適用しました。");
  }

  private void RequestReturnToKiosk()
  {
    if (!TryConfirmLeaveAdmin())
    {
      return;
    }

    ReturnToKioskRequested?.Invoke();
  }

  private void RequestExitApplication()
  {
    if (!TryConfirmLeaveAdmin())
    {
      return;
    }

    var result = MessageBox.Show(
      "アプリを終了します。PC の電源は切れません。よろしいですか？",
      "終了の確認",
      MessageBoxButton.YesNo,
      MessageBoxImage.Question,
      MessageBoxResult.No);

    if (result != MessageBoxResult.Yes)
    {
      return;
    }

    ExitApplicationRequested?.Invoke();
  }

  private void RequestExitAndPowerOff()
  {
    if (!TryConfirmLeaveAdmin())
    {
      return;
    }

    var result = MessageBox.Show(
      "アプリを終了すると、すぐに PC の電源も切れます。よろしいですか？",
      "終了の確認",
      MessageBoxButton.YesNo,
      MessageBoxImage.Warning,
      MessageBoxResult.No);

    if (result != MessageBoxResult.Yes)
    {
      return;
    }

    ExitAndPowerOffRequested?.Invoke();
  }

  /// <summary>
  /// 未保存の確認。表示モードへ戻る・× 閉じから使う。
  /// </summary>
  public bool TryConfirmLeaveAdmin()
  {
    return ConfirmUnsavedChangesBeforeProceed();
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

    var keyChanged = settings.AccessKey != _savedSettings.AccessKey;
    var keyRejected = false;
    if (keyChanged)
    {
      var verify = LicenseService.VerifyAccessKey(settings.AccessKey, _context.Logger);
      if (!verify.IsValid)
      {
        settings.AccessKey = _savedSettings.AccessKey;
        keyRejected = true;
      }
    }

    _context.SettingsService.Save(settings);
    _context.ApplySettings(settings);
    WindowsAutoStartService.Apply(settings.WindowsAutoStart, _context.Logger);
    _applySettings(settings);

    _savedSettings = CloneSettings(settings);
    RefreshLicenseUi();

    if (keyRejected)
    {
      HasValidationError = true;
      StatusMessage = "設定は保存しました。" + LicenseService.FormatInvalidKeyMessage()
                      + " 以前のアクセスキーのままです。";
      return false;
    }

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

    if (requireValid && !SettingsValidator.TryValidate(
          WatchFolderPath.Trim(),
          _savedSettings.AllowNetworkWatchFolder,
          defaultDisplaySeconds,
          AppExitTimeEnabled,
          AppExitTime,
          RecoveryMessage,
          out var errorMessage))
    {
      HasValidationError = true;
      StatusMessage = errorMessage;
      return false;
    }

    settings.WatchFolderPath = WatchFolderPath.Trim();
    settings.AllowNetworkWatchFolder = _savedSettings.AllowNetworkWatchFolder;
    settings.DefaultDisplaySeconds = defaultDisplaySeconds;
    settings.WindowsAutoStart = WindowsAutoStart;
    settings.AppExitTime = AppExitTimeEnabled
      ? SettingsValidator.NormalizeTime(AppExitTime)
      : null;
    settings.RecoveryMessage = RecoveryMessage.Trim();
    settings.AccessKey = string.IsNullOrWhiteSpace(AccessKeyInput)
      ? _savedSettings.AccessKey
      : AccessKeyInput.Trim();
    return true;
  }

  private void RefreshLicenseUi()
  {
    var license = _context.License;
    LicenseStatusText = ToStatusLabel(license.Status);
    LicenseDetailText = FormatLicenseDetail(license);
    LogDirectory = _context.LogDirectory;
  }

  private static string ToStatusLabel(LicenseStatus status)
  {
    return status switch
    {
      LicenseStatus.Trial => "試用中",
      LicenseStatus.TrialExpired => "試用期間終了",
      LicenseStatus.Licensed => "契約中",
      LicenseStatus.LicenseExpired => "契約期限切れ",
      _ => status.ToString()
    };
  }

  private static string FormatLicenseDetail(LicenseEvaluation license)
  {
    var remaining = license.RemainingDays > 0
      ? $"残り {license.RemainingDays} 日"
      : "残り 0 日";
    var text = $"期限 {license.ExpiresOn:yyyy-MM-dd}（{remaining}）";
    if (license.Payload is null)
    {
      return text;
    }

    var plan = license.Payload.Plan == LicensePlan.Site ? "サイト" : "標準";
    if (string.IsNullOrEmpty(license.Payload.Organization))
    {
      return $"{text}  {plan}";
    }

    return $"{text}  {license.Payload.Organization} / {plan}";
  }

  private static bool SettingsEquals(AppSettings left, AppSettings right)
  {
    return left.WatchFolderPath == right.WatchFolderPath
           && left.AllowNetworkWatchFolder == right.AllowNetworkWatchFolder
           && left.DefaultDisplaySeconds == right.DefaultDisplaySeconds
           && left.WindowsAutoStart == right.WindowsAutoStart
           && left.AppExitTime == right.AppExitTime
           && left.RecoveryMessage == right.RecoveryMessage
           && left.AccessKey == right.AccessKey;
  }

  private static AppSettings CloneSettings(AppSettings source)
  {
    return new AppSettings
    {
      WatchFolderPath = source.WatchFolderPath,
      AllowNetworkWatchFolder = source.AllowNetworkWatchFolder,
      DefaultDisplaySeconds = source.DefaultDisplaySeconds,
      WindowsAutoStart = source.WindowsAutoStart,
      AppExitTime = source.AppExitTime,
      RecoveryMessage = source.RecoveryMessage,
      AccessKey = source.AccessKey
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
