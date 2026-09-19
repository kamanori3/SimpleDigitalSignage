namespace PdfSignage.Services;

/// <summary>
/// 運用上の終了。管理画面の明示操作と日次スケジュールから使う。
/// 電源オフ付きは <see cref="Execute"/>、アプリのみは <see cref="ExitProcess"/>。
/// 二重起動の即 Shutdown や自己再起動では使わない。
/// </summary>
public static class AppExit
{
  /// <summary>
  /// 先に PC 電源オフを要求してからプロセスを終了する。
  /// スケジュール終了と、管理画面の「アプリを終了して PC を電源オフ」から使う。
  /// </summary>
  public static void Execute(FileLogger logger, string reason, Action exitProcess)
  {
    Execute(logger, reason, exitProcess, SystemPower.TryPowerOff);
  }

  /// <param name="tryPowerOff">
  /// テスト用。未指定時は <see cref="SystemPower.TryPowerOff(FileLogger)"/>。
  /// </param>
  public static void Execute(
    FileLogger logger,
    string reason,
    Action exitProcess,
    Func<FileLogger, bool> tryPowerOff)
  {
    ArgumentNullException.ThrowIfNull(logger);
    ArgumentException.ThrowIfNullOrWhiteSpace(reason);
    ArgumentNullException.ThrowIfNull(exitProcess);
    ArgumentNullException.ThrowIfNull(tryPowerOff);

    logger.Info($"アプリを終了し、PC を電源オフします（{reason}）。");

    var poweredOff = false;
    try
    {
      poweredOff = tryPowerOff(logger);
    }
    catch (Exception ex)
    {
      logger.Error("PC 電源オフコマンドの実行に失敗しました。", ex);
    }

    if (!poweredOff)
    {
      logger.Error("PC 電源オフに失敗しました。アプリのみ終了します。");
    }

    exitProcess();
  }

  /// <summary>
  /// アプリだけ終了する。PC は落とさない。管理画面の「アプリ終了」から使う。
  /// </summary>
  public static void ExitProcess(FileLogger logger, string reason, Action exitProcess)
  {
    ArgumentNullException.ThrowIfNull(logger);
    ArgumentException.ThrowIfNullOrWhiteSpace(reason);
    ArgumentNullException.ThrowIfNull(exitProcess);

    logger.Info($"アプリを終了します（{reason}）。PC は電源オフしません。");
    exitProcess();
  }
}
