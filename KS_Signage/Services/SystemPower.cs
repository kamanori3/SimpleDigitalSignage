using System.Diagnostics;

namespace KS_Signage.Services;

/// <summary>
/// PC の電源オフ。運用終了（<see cref="AppExit"/>）からのみ呼ぶ。
/// グループポリシーにより <c>shutdown</c> が拒否される場合がある。
/// </summary>
public static class SystemPower
{
  public const string ShutdownFileName = "shutdown.exe";
  public const string ShutdownArguments = "/s /t 0 /f";

  /// <summary>
  /// 既にシャットダウンが予約済み／進行中。二重実行時は成功扱いする。
  /// </summary>
  private const int ErrorShutdownInProgress = 1115;
  private const int ErrorShutdownIsScheduled = 1190;

  public static bool TryPowerOff(FileLogger logger)
  {
    return TryPowerOff(logger, ExecuteShutdown);
  }

  /// <param name="executeShutdown">
  /// テスト用。未指定時は <c>shutdown /s /t 0 /f</c> を起動する。
  /// true なら OS が電源オフ要求を受け付けたものとする。
  /// </param>
  public static bool TryPowerOff(FileLogger logger, Func<bool> executeShutdown)
  {
    try
    {
      if (!executeShutdown())
      {
        logger.Error("PC 電源オフコマンドの起動に失敗しました。");
        return false;
      }

      logger.Info($"PC 電源オフコマンドを実行しました（shutdown {ShutdownArguments}）。");
      return true;
    }
    catch (Exception ex)
    {
      logger.Error("PC 電源オフコマンドの実行に失敗しました。", ex);
      return false;
    }
  }

  public static ProcessStartInfo CreateShutdownStartInfo()
  {
    return new ProcessStartInfo
    {
      FileName = Path.Combine(Environment.SystemDirectory, ShutdownFileName),
      Arguments = ShutdownArguments,
      CreateNoWindow = true,
      UseShellExecute = false,
      RedirectStandardError = true,
      RedirectStandardOutput = true
    };
  }

  private static bool ExecuteShutdown()
  {
    using var process = Process.Start(CreateShutdownStartInfo());
    if (process is null)
    {
      return false;
    }

    process.StandardOutput.ReadToEnd();
    process.StandardError.ReadToEnd();
    if (!process.WaitForExit(10_000))
    {
      return false;
    }

    return process.ExitCode is 0 or ErrorShutdownInProgress or ErrorShutdownIsScheduled;
  }
}
