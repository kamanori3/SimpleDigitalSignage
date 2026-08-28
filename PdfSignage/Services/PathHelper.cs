namespace PdfSignage.Services;

public static class PathHelper
{
  /// <summary>
  /// 監視フォルダと同じ階層の logs フォルダパスを返す。
  /// 例: D:\Signage → D:\logs
  /// </summary>
  public static string GetLogDirectory(string watchFolderPath)
  {
    var fullPath = Path.GetFullPath(watchFolderPath);
    var parent = Directory.GetParent(fullPath);

    if (parent is null)
    {
      return Path.Combine(fullPath, "logs");
    }

    return Path.Combine(parent.FullName, "logs");
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
  public static string ResolveWatchFolderPath(string configuredPath)
  {
    if (Directory.Exists(configuredPath))
    {
      return Path.GetFullPath(configuredPath);
    }

    var devPath = GetDevSignageDataPath();
    Directory.CreateDirectory(devPath);
    return devPath;
  }
}
