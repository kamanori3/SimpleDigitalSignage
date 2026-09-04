using PdfSignage.Models;

namespace PdfSignage.Services;

public static class PathHelper
{
  /// <summary>
  /// 監視フォルダと同じ階層の logs フォルダパスを返す。
  /// 例: D:\Signage → D:\logs
  /// </summary>
  public static string GetLogDirectory(string watchFolderPath)
  {
    // 末尾の区切り文字が残っていると GetParent が監視フォルダ自身を返し、
    // logs が監視フォルダの内側に作られてしまう
    var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(watchFolderPath));
    var parent = Directory.GetParent(fullPath);

    if (parent is null)
    {
      return Path.Combine(fullPath, "logs");
    }

    return Path.Combine(parent.FullName, "logs");
  }

  /// <summary>
  /// 設定上の監視フォルダに基づいてログパスを算出する。
  /// 設定パスのドライブが存在しない開発環境では、実際の監視フォルダに基づく。
  /// </summary>
  public static string ResolveLogDirectory(string configuredWatchFolder, string resolvedWatchFolder)
  {
    var configRoot = Path.GetPathRoot(Path.GetFullPath(configuredWatchFolder));
    if (configRoot is not null && !Directory.Exists(configRoot))
    {
      return GetLogDirectory(resolvedWatchFolder);
    }

    return GetLogDirectory(configuredWatchFolder);
  }

  /// <summary>
  /// 開発用フォルダ（exe 横の SignageData）
  /// </summary>
  public static string GetDevSignageDataPath()
  {
    return Path.Combine(AppContext.BaseDirectory, "SignageData");
  }

  /// <summary>
  /// Google Drive 同期キャッシュ（exe 横の DriveCache）
  /// </summary>
  public static string GetDriveCacheDirectory()
  {
    return Path.Combine(AppContext.BaseDirectory, "DriveCache");
  }

  /// <summary>
  /// 表示に使うフォルダ。Drive URL 指定時はキャッシュ、それ以外は監視フォルダ。
  /// </summary>
  public static string ResolveContentFolder(AppSettings settings)
  {
    if (settings.UsesGoogleDrive)
    {
      var cache = GetDriveCacheDirectory();
      Directory.CreateDirectory(cache);
      return cache;
    }

    return ResolveWatchFolderPath(settings.WatchFolderPath);
  }

  /// <summary>
  /// 監視フォルダが存在しない場合、開発用パスへフォールバックする。
  /// </summary>
  public static string ResolveWatchFolderPath(string configuredPath)
  {
    if (Directory.Exists(configuredPath))
    {
      return Path.GetFullPath(configuredPath);
    }

    var configRoot = Path.GetPathRoot(Path.GetFullPath(configuredPath));
    if (configRoot is not null && Directory.Exists(configRoot))
    {
      var fullPath = Path.GetFullPath(configuredPath);
      Directory.CreateDirectory(fullPath);
      return fullPath;
    }

    var devPath = GetDevSignageDataPath();
    Directory.CreateDirectory(devPath);
    return devPath;
  }
}
