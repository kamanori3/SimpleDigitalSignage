using System.Diagnostics;

namespace PdfSignage.Services;

/// <summary>
/// PC 電源オフ（shutdown コマンド）の実行。
/// 標準ユーザーでも多くの環境で実行可能だが、グループポリシーにより拒否される場合がある。
/// </summary>
public static class PcShutdownService
{
  public static bool TryShutdown(FileLogger logger)
  {
    try
    {
      var startInfo = new ProcessStartInfo
      {
        FileName = "shutdown",
        Arguments = "/s /t 0",
        CreateNoWindow = true,
        UseShellExecute = false
      };

      using var process = Process.Start(startInfo);
      if (process is null)
      {
        logger.Error("PC 電源オフコマンドの起動に失敗しました（プロセスが null）。");
        return false;
      }

      logger.Info("PC 電源オフコマンドを実行しました（shutdown /s /t 0）。");
      return true;
    }
    catch (Exception ex)
    {
      logger.Error("PC 電源オフコマンドの実行に失敗しました。", ex);
      return false;
    }
  }
}
