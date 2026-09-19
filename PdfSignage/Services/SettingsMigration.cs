using PdfSignage.Models;

namespace PdfSignage.Services;

/// <summary>
/// 旧設定の読み替え。アプリ終了と PC 電源オフは同一時刻になった。
/// </summary>
public static class SettingsMigration
{
  /// <summary>
  /// 旧 <c>pcShutdownTime</c> を <see cref="AppSettings.AppExitTime"/> へ統合する。
  /// 両方あるときは運用終了時刻である <see cref="AppSettings.AppExitTime"/> を残す。
  /// </summary>
  public static void UnifyLegacyExitTimes(AppSettings settings)
  {
    if (string.IsNullOrWhiteSpace(settings.AppExitTime)
        && !string.IsNullOrWhiteSpace(settings.PcShutdownTime))
    {
      settings.AppExitTime = settings.PcShutdownTime.Trim();
    }
    else if (!string.IsNullOrWhiteSpace(settings.AppExitTime))
    {
      settings.AppExitTime = settings.AppExitTime.Trim();
    }
    else
    {
      settings.AppExitTime = null;
    }

    settings.PcShutdownTime = null;
  }
}
