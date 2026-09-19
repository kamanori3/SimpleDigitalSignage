using System.Text.RegularExpressions;
using KS_Signage.Models;

namespace KS_Signage.Services;

/// <summary>
/// 管理画面から入力された設定値のバリデーション。
/// </summary>
public static partial class SettingsValidator
{
  [GeneratedRegex(@"^([01]?\d|2[0-3]):([0-5]\d)$", RegexOptions.CultureInvariant)]
  private static partial Regex TimePattern();

  public static bool TryValidate(
    string watchFolderPath,
    bool allowNetworkWatchFolder,
    int defaultDisplaySeconds,
    bool appExitTimeEnabled,
    string appExitTime,
    string recoveryMessage,
    out string errorMessage,
    Func<string, DriveType>? getDriveType = null)
  {
    if (string.IsNullOrWhiteSpace(watchFolderPath))
    {
      errorMessage = "監視フォルダを入力してください。";
      return false;
    }

    if (!WatchFolderLocationPolicy.IsAllowedOnThisPc(
          watchFolderPath, allowNetworkWatchFolder, getDriveType))
    {
      errorMessage = "監視フォルダはこの PC 内のフォルダを指定してください。";
      return false;
    }

    if (defaultDisplaySeconds is < AppSettings.MinDisplaySeconds or > AppSettings.MaxDisplaySeconds)
    {
      errorMessage =
        $"デフォルト表示秒数は {AppSettings.MinDisplaySeconds}〜{AppSettings.MaxDisplaySeconds} 秒の範囲で入力してください。";
      return false;
    }

    if (appExitTimeEnabled && !IsValidTime(appExitTime))
    {
      errorMessage = "終了時刻は HH:mm 形式（例: 18:00）で入力してください。";
      return false;
    }

    if (string.IsNullOrWhiteSpace(recoveryMessage))
    {
      errorMessage = "復帰不能時メッセージを入力してください。";
      return false;
    }

    errorMessage = "";
    return true;
  }

  public static bool IsValidTime(string value)
  {
    return TimePattern().IsMatch(value.Trim());
  }

  public static string NormalizeTime(string value) => value.Trim();
}
