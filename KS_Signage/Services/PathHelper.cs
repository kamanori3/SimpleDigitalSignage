namespace KS_Signage.Services;

public static class PathHelper
{
  /// <summary>
  /// この PC のログフォルダ。監視フォルダ（共有を含む）には置かない。
  /// </summary>
  public static string GetLogDirectory()
  {
    return Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "KS_Signage",
      "logs");
  }

  /// <summary>
  /// 開発用フォルダ（exe 横の SignageData）
  /// </summary>
  public static string GetDevSignageDataPath()
  {
    return Path.Combine(AppContext.BaseDirectory, "SignageData");
  }

  /// <summary>
  /// 監視フォルダが存在しない場合、開発用パスへフォールバックする。
  /// </summary>
  public static string ResolveWatchFolderPath(
    string configuredPath,
    bool allowNetworkWatchFolder = false)
  {
    if (!WatchFolderLocationPolicy.IsAllowedOnThisPc(configuredPath, allowNetworkWatchFolder))
    {
      var rejectedPath = GetDevSignageDataPath();
      Directory.CreateDirectory(rejectedPath);
      return rejectedPath;
    }

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
