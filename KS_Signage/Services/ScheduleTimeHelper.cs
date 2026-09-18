using System.Globalization;

namespace KS_Signage.Services;

/// <summary>
/// スケジュール時刻（HH:mm）の計算ヘルパー。
/// </summary>
public static class ScheduleTimeHelper
{
  public const int DefaultPcShutdownDelayMinutes = 3;

  /// <summary>
  /// アプリ終了時に PC 電源オフを予約してよい最大待ち時間。
  /// 翌朝の営業開始時刻までプロセス無しで待たないための上限。
  /// </summary>
  public const int MaxShutdownArmDelayHours = 12;

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

  /// <summary>
  /// いまから PC 電源オフ時刻までの秒数を返す。
  /// 当日中（日付跨ぎ含む）で、かつ <see cref="MaxShutdownArmDelayHours"/> 時間以内のときだけ true。
  /// アプリ終了後も OS 側で電源オフできるように予約するため。
  /// </summary>
  public static bool TryGetPendingShutdownDelaySeconds(
    DateTime now,
    string? pcShutdownTime,
    out int delaySeconds)
  {
    delaySeconds = 0;
    if (!TryParseScheduleTime(pcShutdownTime, out var timeOfDay))
    {
      return false;
    }

    var shutdownAt = now.Date + timeOfDay;
    if (shutdownAt < now)
    {
      shutdownAt = shutdownAt.AddDays(1);
    }

    var delay = shutdownAt - now;
    if (delay > TimeSpan.FromHours(MaxShutdownArmDelayHours))
    {
      return false;
    }

    delaySeconds = (int)Math.Ceiling(delay.TotalSeconds);
    if (delaySeconds < 0)
    {
      delaySeconds = 0;
    }

    return true;
  }
}
