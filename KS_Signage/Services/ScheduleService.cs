using System.Windows.Threading;
using KS_Signage.Models;

namespace KS_Signage.Services;

/// <summary>
/// 日次のアプリ終了・PC 電源オフ時刻を監視するスケジューラ。
/// 日付跨ぎも検知し、再生期限の再評価用に <see cref="DateRolledOver"/> を発火する。
/// </summary>
public sealed class ScheduleService : IDisposable
{
  private const int CheckIntervalSeconds = 30;

  private readonly ApplicationContext _context;
  private readonly DispatcherTimer _timer;
  private TimeSpan? _previousCheckTimeOfDay;
  private DateOnly _previousCheckDate;

  public ScheduleService(ApplicationContext context)
  {
    _context = context;
    _timer = new DispatcherTimer
    {
      Interval = TimeSpan.FromSeconds(CheckIntervalSeconds)
    };
    _timer.Tick += OnTimerTick;
  }

  public event Action? AppExitRequested;
  public event Action? PcShutdownRequested;
  public event Action? DateRolledOver;

  /// <summary>前回と今回の日付が異なれば true（スリープ越しの日付変更も含む）。</summary>
  public static bool HasDateChanged(DateOnly previousDate, DateOnly currentDate)
  {
    return currentDate != previousDate;
  }

  public void Start()
  {
    var now = DateTime.Now;
    _previousCheckTimeOfDay = now.TimeOfDay;
    _previousCheckDate = DateOnly.FromDateTime(now);
    LogActiveSchedule();
    _timer.Start();
    CheckSchedule(_previousCheckTimeOfDay.Value, now.TimeOfDay);
  }

  public void Dispose()
  {
    _timer.Tick -= OnTimerTick;
    _timer.Stop();
  }

  private void OnTimerTick(object? sender, EventArgs e)
  {
    var now = DateTime.Now;
    var previous = _previousCheckTimeOfDay ?? now.TimeOfDay;
    var current = now.TimeOfDay;
    var currentDate = DateOnly.FromDateTime(now);

    if (HasDateChanged(_previousCheckDate, currentDate))
    {
      _context.Logger.Info("日付が変わりました。プレイリスト再構築を予約します。");
      DateRolledOver?.Invoke();
    }

    CheckSchedule(previous, current);
    _previousCheckTimeOfDay = current;
    _previousCheckDate = currentDate;
  }

  private void CheckSchedule(TimeSpan previous, TimeSpan current)
  {
    var settings = _context.Settings;

    if (ScheduleTimeHelper.TryParseScheduleTime(settings.AppExitTime, out var appExitTime) &&
        CrossedScheduleTime(previous, current, appExitTime))
    {
      _context.Logger.Info($"スケジュール: アプリ終了時刻（{settings.AppExitTime}）に到達しました。");
      AppExitRequested?.Invoke();
    }

    if (ScheduleTimeHelper.TryParseScheduleTime(settings.PcShutdownTime, out var pcShutdownTime) &&
        CrossedScheduleTime(previous, current, pcShutdownTime))
    {
      _context.Logger.Info($"スケジュール: PC 電源オフ時刻（{settings.PcShutdownTime}）に到達しました。");
      PcShutdownRequested?.Invoke();
    }
  }

  /// <summary>
  /// 前回チェックから今回チェックの間に、指定時刻を跨いだかどうか。
  /// 起動直後に当日の時刻を過ぎている場合は発火しない。
  /// </summary>
  private static bool CrossedScheduleTime(TimeSpan previous, TimeSpan current, TimeSpan scheduled)
  {
    if (previous <= current)
    {
      return previous < scheduled && current >= scheduled;
    }

    // 日付跨ぎ（例: 23:59 → 00:01）
    return previous < scheduled || current >= scheduled;
  }

  private void LogActiveSchedule()
  {
    var settings = _context.Settings;
    if (settings.AppExitTime is not null)
    {
      _context.Logger.Info($"スケジュール監視: アプリ終了={settings.AppExitTime}");
    }

    if (settings.PcShutdownTime is not null)
    {
      _context.Logger.Info($"スケジュール監視: PC 電源オフ={settings.PcShutdownTime}");
    }

    if (settings.AppExitTime is null && settings.PcShutdownTime is null)
    {
      _context.Logger.Info("スケジュール監視: 有効な時刻設定がありません。");
    }
  }
}
