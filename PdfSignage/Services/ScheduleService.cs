using System.Windows.Threading;
using PdfSignage.Models;

namespace PdfSignage.Services;

/// <summary>
/// 日次のアプリ終了・PC 電源オフ時刻を監視するスケジューラ。
/// </summary>
public sealed class ScheduleService : IDisposable
{
  private const int CheckIntervalSeconds = 30;

  private readonly ApplicationContext _context;
  private readonly DispatcherTimer _timer;
  private TimeSpan? _previousCheckTimeOfDay;

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

  public void Start()
  {
    _previousCheckTimeOfDay = DateTime.Now.TimeOfDay;
    LogActiveSchedule();
    _timer.Start();
    CheckSchedule(_previousCheckTimeOfDay!.Value, DateTime.Now.TimeOfDay);
  }

  public void Dispose()
  {
    _timer.Tick -= OnTimerTick;
    _timer.Stop();
  }

  private void OnTimerTick(object? sender, EventArgs e)
  {
    var previous = _previousCheckTimeOfDay ?? DateTime.Now.TimeOfDay;
    var current = DateTime.Now.TimeOfDay;
    CheckSchedule(previous, current);
    _previousCheckTimeOfDay = current;
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
