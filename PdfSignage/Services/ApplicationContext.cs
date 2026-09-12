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

  private ApplicationContext(
    AppSettings settings,
    string resolvedWatchFolder,
    string logDirectory,
    FileLogger logger,
    SettingsService settingsService)
  {
    Settings = settings;
    ResolvedWatchFolder = resolvedWatchFolder;
    LogDirectory = logDirectory;
    Logger = logger;
    SettingsService = settingsService;
  }

  public static ApplicationContext Initialize()
  {
    var settingsService = new SettingsService();
    var settings = settingsService.Load();
    var resolvedWatchFolder = PathHelper.ResolveWatchFolderPath(
      settings.WatchFolderPath, settings.AllowNetworkWatchFolder);
    var logDirectory = PathHelper.ResolveLogDirectory(
      settings.WatchFolderPath, resolvedWatchFolder, settings.AllowNetworkWatchFolder);

    Directory.CreateDirectory(resolvedWatchFolder);
    Directory.CreateDirectory(logDirectory);

    var logger = new FileLogger(logDirectory);
    var context = new ApplicationContext(
      settings,
      resolvedWatchFolder,
      logDirectory,
      logger,
      settingsService);

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
    LogDirectory = PathHelper.ResolveLogDirectory(
      settings.WatchFolderPath, ResolvedWatchFolder, settings.AllowNetworkWatchFolder);

    Directory.CreateDirectory(ResolvedWatchFolder);
    Directory.CreateDirectory(LogDirectory);
    Logger.SetLogDirectory(LogDirectory);

    Logger.Info($"設定反映: 監視フォルダ（設定）={settings.WatchFolderPath}");
    Logger.Info($"設定反映: 監視フォルダ（実際）={ResolvedWatchFolder}");
    LogNetworkWatchFolderFallback(Logger, settings, ResolvedWatchFolder);
    Logger.Info($"設定反映: ログフォルダ={LogDirectory}");
    Logger.Info($"設定反映: デフォルト表示秒数={settings.DefaultDisplaySeconds} 秒");
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
