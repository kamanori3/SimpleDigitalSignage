using System.Globalization;

namespace PdfSignage.Services;

/// <summary>
/// スケジュール時刻（HH:mm）の解析。
/// </summary>
public static class ScheduleTimeHelper
{
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
