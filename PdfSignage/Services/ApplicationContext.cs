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
    var resolvedWatchFolder = PathHelper.ResolveWatchFolderPath(settings.WatchFolderPath);
    var logDirectory = PathHelper.ResolveLogDirectory(settings.WatchFolderPath, resolvedWatchFolder);

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
    ResolvedWatchFolder = PathHelper.ResolveWatchFolderPath(settings.WatchFolderPath);
    LogDirectory = PathHelper.ResolveLogDirectory(settings.WatchFolderPath, ResolvedWatchFolder);

    Directory.CreateDirectory(ResolvedWatchFolder);
    Directory.CreateDirectory(LogDirectory);
    Logger.SetLogDirectory(LogDirectory);

    Logger.Info($"設定反映: 監視フォルダ（設定）={settings.WatchFolderPath}");
    Logger.Info($"設定反映: 監視フォルダ（実際）={ResolvedWatchFolder}");
    Logger.Info($"設定反映: ログフォルダ={LogDirectory}");
    Logger.Info($"設定反映: デフォルト表示秒数={settings.DefaultDisplaySeconds} 秒");
  }
}
