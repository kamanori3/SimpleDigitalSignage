using System.Windows;
using PdfSignage.Models;
using PdfSignage.Services;

namespace PdfSignage;

public partial class App : Application
{
  public static AppSettings Settings { get; private set; } = new();
  public static string ResolvedWatchFolder { get; private set; } = "";
  public static string LogDirectory { get; private set; } = "";
  public static FileLogger Logger { get; private set; } = null!;

  private void Application_Startup(object sender, StartupEventArgs e)
  {
    var settingsService = new SettingsService();
    Settings = settingsService.Load();
    ResolvedWatchFolder = PathHelper.ResolveWatchFolderPath(Settings.WatchFolderPath);
    LogDirectory = PathHelper.GetLogDirectory(ResolvedWatchFolder);

    Logger = new FileLogger(LogDirectory);
    Logger.Info($"アプリケーション起動 (v{typeof(App).Assembly.GetName().Version})");
    Logger.Info($"設定ファイル: {settingsService.SettingsFilePath}");
    Logger.Info($"監視フォルダ: {ResolvedWatchFolder}");
    Logger.Info($"ログフォルダ: {LogDirectory}");

    Directory.CreateDirectory(ResolvedWatchFolder);

    var mainWindow = new MainWindow();
    mainWindow.Show();
  }
}
