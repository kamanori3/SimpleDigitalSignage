using System.Diagnostics;

namespace PdfSignage.Services;

/// <summary>
/// PC の電源オフ。運用終了（<see cref="AppExit"/>）からのみ呼ぶ。
/// グループポリシーにより <c>shutdown</c> が拒否される場合がある。
/// </summary>
public static class SystemPower
{
  public const string ShutdownFileName = "shutdown";
  public const string ShutdownArguments = "/s /t 0";

  public static bool TryPowerOff(FileLogger logger)
  {
    return TryPowerOff(logger, ExecuteShutdown);
  }

  /// <param name="executeShutdown">
  /// テスト用。未指定時は <c>shutdown /s /t 0</c> を起動する。
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

      logger.Info($"PC 電源オフコマンドを実行しました（{ShutdownFileName} {ShutdownArguments}）。");
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
      FileName = ShutdownFileName,
      Arguments = ShutdownArguments,
      CreateNoWindow = true,
      UseShellExecute = false
    };
  }

  private static bool ExecuteShutdown()
  {
    using var process = Process.Start(CreateShutdownStartInfo());
    return process is not null;
  }
}
