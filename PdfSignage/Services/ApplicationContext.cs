using PdfSignage.Models;

namespace PdfSignage.Services;

/// <summary>
/// アプリケーション全体で共有するサービスと設定のコンテナ
/// </summary>
public sealed class ApplicationContext : IDisposable
{
  public static ApplicationContext? Current { get; private set; }

  public AppSettings Settings { get; private set; }
  public string ResolvedWatchFolder { get; private set; }
  public string LogDirectory { get; private set; }
  public FileLogger Logger { get; }
  public SettingsService SettingsService { get; }

  private DriveFolderSyncService? _driveSync;

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
    var (resolvedWatchFolder, logDirectory) = ResolvePaths(settings);

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
    LogContentSource(logger, settings, resolvedWatchFolder, logDirectory);

    WindowsAutoStartService.Apply(settings.WindowsAutoStart, logger);
    context.RestartDriveSync();

    Current = context;
    return context;
  }

  /// <summary>
  /// 管理画面から保存された設定をランタイムへ反映する。
  /// </summary>
  public void ApplySettings(AppSettings settings)
  {
    Settings = settings;
    var (resolvedWatchFolder, logDirectory) = ResolvePaths(settings);
    ResolvedWatchFolder = resolvedWatchFolder;
    LogDirectory = logDirectory;

    Directory.CreateDirectory(ResolvedWatchFolder);
    Directory.CreateDirectory(LogDirectory);
    Logger.SetLogDirectory(LogDirectory);

    LogContentSource(Logger, settings, ResolvedWatchFolder, LogDirectory);
    Logger.Info($"設定反映: デフォルト表示秒数={settings.DefaultDisplaySeconds} 秒");
    RestartDriveSync();
  }

  public void Dispose()
  {
    _driveSync?.Dispose();
    _driveSync = null;
  }

  private void RestartDriveSync()
  {
    _driveSync?.Dispose();
    _driveSync = null;

    if (!Settings.UsesGoogleDrive)
    {
      return;
    }

    if (!DriveFolderUrlParser.TryParseFolderId(Settings.GoogleDriveFolderUrl, out var folderId)
        || string.IsNullOrWhiteSpace(Settings.GoogleDriveApiKey))
    {
      Logger.Error("Google Drive の URL または API キーが不正なため同期を開始できません。");
      return;
    }

    _driveSync = new DriveFolderSyncService(
      Logger,
      PathHelper.GetDriveCacheDirectory(),
      Settings.GoogleDriveApiKey,
      folderId,
      Settings.GoogleDriveSyncIntervalMinutes);
    _driveSync.Start();
    Logger.Info($"Google Drive 同期を開始しました（間隔 {Settings.GoogleDriveSyncIntervalMinutes} 分）。");
  }

  private static (string ContentFolder, string LogDirectory) ResolvePaths(AppSettings settings)
  {
    var contentFolder = PathHelper.ResolveContentFolder(settings);
    var logDirectory = settings.UsesGoogleDrive
      ? PathHelper.GetLogDirectory(contentFolder)
      : PathHelper.ResolveLogDirectory(settings.WatchFolderPath, contentFolder);
    return (contentFolder, logDirectory);
  }

  private static void LogContentSource(
    FileLogger logger,
    AppSettings settings,
    string resolvedWatchFolder,
    string logDirectory)
  {
    if (settings.UsesGoogleDrive)
    {
      logger.Info("コンテンツ元: Google Drive（公開フォルダをキャッシュへ同期）");
      logger.Info($"Drive フォルダ URL: {settings.GoogleDriveFolderUrl}");
    }
    else
    {
      logger.Info($"監視フォルダ（設定）: {settings.WatchFolderPath}");
    }

    logger.Info($"監視フォルダ（実際）: {resolvedWatchFolder}");
    logger.Info($"ログフォルダ: {logDirectory}");
  }
}
