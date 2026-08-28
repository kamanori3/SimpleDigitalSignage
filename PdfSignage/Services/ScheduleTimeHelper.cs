using System.Globalization;

namespace PdfSignage.Services;

/// <summary>
/// スケジュール時刻（HH:mm）の計算ヘルパー。
/// </summary>
public static class ScheduleTimeHelper
{
  public const int DefaultPcShutdownDelayMinutes = 3;

  /// <summary>
  /// 指定時刻に分を加算し、HH:mm 形式で返す（日付跨ぎ対応）。
  /// </summary>
  public static string AddMinutes(string time, int minutes)
  {
    if (!TimeSpan.TryParseExact(time.Trim(), "g", CultureInfo.InvariantCulture, out var timeOfDay))
    {
      return time;
    }

    var totalMinutes = (int)timeOfDay.TotalMinutes + minutes;
    totalMinutes = ((totalMinutes % (24 * 60)) + (24 * 60)) % (24 * 60);

    var result = TimeSpan.FromMinutes(totalMinutes);
    return $"{result.Hours:D2}:{result.Minutes:D2}";
  }

  /// <summary>
  /// アプリ終了時刻から PC シャットダウンのデフォルト時刻を算出する。
  /// </summary>
  public static string GetDefaultPcShutdownTime(string appExitTime)
  {
    return AddMinutes(appExitTime, DefaultPcShutdownDelayMinutes);
  }

  /// <summary>
  /// HH:mm 形式の時刻を TimeSpan に変換する。
  /// </summary>
  public static bool TryParseScheduleTime(string? time, out TimeSpan timeOfDay)
  {
    if (string.IsNullOrWhiteSpace(time))
    {
      timeOfDay = default;
      return false;
    }

    return TimeSpan.TryParseExact(
      time.Trim(),
      "g",
      CultureInfo.InvariantCulture,
      out timeOfDay);
  }
}
