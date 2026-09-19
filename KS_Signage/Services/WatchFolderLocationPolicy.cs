namespace KS_Signage.Services;

/// <summary>
/// 監視フォルダを「この PC 内」に限定するかの判定。
/// ネットワーク商品は <see cref="Models.AppSettings.AllowNetworkWatchFolder"/> を true にしてゲートを外す。
/// </summary>
public static class WatchFolderLocationPolicy
{
  /// <summary>
  /// 指定パスを監視フォルダとして使ってよいか。
  /// <paramref name="getDriveType"/> はテスト用。未指定時は <see cref="DriveInfo"/> を使う。
  /// </summary>
  public static bool IsAllowedOnThisPc(
    string path,
    bool allowNetworkWatchFolder,
    Func<string, DriveType>? getDriveType = null)
  {
    if (allowNetworkWatchFolder)
    {
      return true;
    }

    if (string.IsNullOrWhiteSpace(path))
    {
      return false;
    }

    var trimmed = path.Trim();
    if (IsUncPath(trimmed))
    {
      return false;
    }

    try
    {
      var fullPath = Path.GetFullPath(trimmed);
      var root = Path.GetPathRoot(fullPath);
      if (string.IsNullOrEmpty(root))
      {
        return false;
      }

      var driveType = getDriveType is not null
        ? getDriveType(root)
        : new DriveInfo(root).DriveType;

      return driveType != DriveType.Network;
    }
    catch (Exception)
    {
      return false;
    }
  }

  public static bool IsUncPath(string path)
  {
    if (path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    if (path.StartsWith(@"\\?\", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    if (path.StartsWith(@"\\", StringComparison.Ordinal) ||
        path.StartsWith("//", StringComparison.Ordinal))
    {
      return true;
    }

    return Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.IsUnc;
  }
}
