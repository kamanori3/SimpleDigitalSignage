using PdfSignage.Licensing;
using PdfSignage.Models;

namespace PdfSignage.Services;

/// <summary>
/// アプリケーション全体で共有するサービスと設定のコンテナ
/// </summary>
public sealed class ApplicationContext
{
  public static ApplicationContext? Current { get; private set; }

  public AppSettings Settings { get; private set; }
  public string ResolvedWatchFolder { get; private set; }
  public string LogDirectory { get; private set; }
  public FileLogger Logger { get; }
  public SettingsService SettingsService { get; }
  public LicenseEvaluation License { get; private set; }

  private ApplicationContext(
    AppSettings settings,
    string resolvedWatchFolder,
    string logDirectory,
    FileLogger logger,
    SettingsService settingsService,
    LicenseEvaluation license)
  {
    Settings = settings;
    ResolvedWatchFolder = resolvedWatchFolder;
    LogDirectory = logDirectory;
    Logger = logger;
    SettingsService = settingsService;
    License = license;
  }

  public static ApplicationContext Initialize()
  {
    var settingsService = new SettingsService();
    var settings = settingsService.Load();
    var resolvedWatchFolder = PathHelper.ResolveWatchFolderPath(
      settings.WatchFolderPath, settings.AllowNetworkWatchFolder);
    var logDirectory = PathHelper.GetLogDirectory();

    Directory.CreateDirectory(resolvedWatchFolder);
    Directory.CreateDirectory(logDirectory);

    var logger = new FileLogger(logDirectory);
    var license = EvaluateLicenseSafely(settings.AccessKey, logger);
    var context = new ApplicationContext(
      settings,
      resolvedWatchFolder,
      logDirectory,
      logger,
      settingsService,
      license);

    logger.Info($"アプリケーション起動 (v{typeof(ApplicationContext).Assembly.GetName().Version})");
    logger.Info($"設定ファイル: {settingsService.SettingsFilePath}");
    logger.Info($"監視フォルダ（設定）: {settings.WatchFolderPath}");
    logger.Info($"監視フォルダ（実際）: {resolvedWatchFolder}");
    LogNetworkWatchFolderFallback(logger, settings, resolvedWatchFolder);
    logger.Info($"ログフォルダ: {logDirectory}");

    WindowsAutoStartService.Apply(settings.WindowsAutoStart, logger);

    Current = context;
    return context;
  }

  /// <summary>
  /// 管理画面から保存された設定をランタイムへ反映する。
  /// </summary>
  public void ApplySettings(AppSettings settings)
  {
    Settings = settings;
    ResolvedWatchFolder = PathHelper.ResolveWatchFolderPath(
      settings.WatchFolderPath, settings.AllowNetworkWatchFolder);
    LogDirectory = PathHelper.GetLogDirectory();

    Directory.CreateDirectory(ResolvedWatchFolder);
    Directory.CreateDirectory(LogDirectory);
    Logger.SetLogDirectory(LogDirectory);

    Logger.Info($"設定反映: 監視フォルダ（設定）={settings.WatchFolderPath}");
    Logger.Info($"設定反映: 監視フォルダ（実際）={ResolvedWatchFolder}");
    LogNetworkWatchFolderFallback(Logger, settings, ResolvedWatchFolder);
    Logger.Info($"設定反映: ログフォルダ={LogDirectory}");
    Logger.Info($"設定反映: デフォルト表示秒数={settings.DefaultDisplaySeconds} 秒");
    RefreshLicense();
  }

  /// <summary>
  /// 試用開始日とアクセスキーから課金状態を再計算する。失敗しても前回の状態を残す。
  /// </summary>
  public void RefreshLicense(DateOnly? today = null)
  {
    try
    {
      License = LicenseService.Evaluate(Settings.AccessKey, Logger, today);
    }
    catch (Exception ex)
    {
      Logger.Error("ライセンス状態の再計算に失敗しました。前回の状態を維持します。", ex);
    }
  }

  private static LicenseEvaluation EvaluateLicenseSafely(string? accessKey, FileLogger logger)
  {
    try
    {
      return LicenseService.Evaluate(accessKey, logger);
    }
    catch (Exception ex)
    {
      logger.Error("ライセンス状態の判定に失敗したため、試用として起動します。", ex);
      var today = DateOnly.FromDateTime(DateTime.Now);
      return LicenseStateEvaluator.Evaluate(AccessKeyVerifyResult.Invalid, today, today);
    }
  }

  private static void LogNetworkWatchFolderFallback(
    FileLogger logger,
    AppSettings settings,
    string resolvedWatchFolder)
  {
    if (WatchFolderLocationPolicy.IsAllowedOnThisPc(
          settings.WatchFolderPath, settings.AllowNetworkWatchFolder))
    {
      return;
    }

    logger.Info(
      $"監視フォルダがこの PC 内ではないため、開発用フォルダへフォールバックしました: {resolvedWatchFolder}");
  }
}
