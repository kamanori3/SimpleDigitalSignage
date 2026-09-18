using System.Diagnostics;

namespace KS_Signage.Services;

/// <summary>
/// PC 電源オフ（shutdown コマンド）の実行。
/// 標準ユーザーでも多くの環境で実行可能だが、グループポリシーにより拒否される場合がある。
/// </summary>
public static class PcShutdownService
{
  /// <summary>
  /// 既にシャットダウンが予約済み／進行中。二重実行時は成功扱いする。
  /// </summary>
  private const int ErrorShutdownInProgress = 1115;
  private const int ErrorShutdownIsScheduled = 1190;

  public static bool TryShutdown(FileLogger logger, int delaySeconds = 0)
  {
    if (delaySeconds < 0)
    {
      delaySeconds = 0;
    }

    var arguments = BuildArguments(delaySeconds);
    var shutdownExe = Path.Combine(Environment.SystemDirectory, "shutdown.exe");

    try
    {
      var startInfo = new ProcessStartInfo
      {
        FileName = shutdownExe,
        Arguments = arguments,
        CreateNoWindow = true,
        UseShellExecute = false,
        RedirectStandardError = true,
        RedirectStandardOutput = true
      };

      using var process = Process.Start(startInfo);
      if (process is null)
      {
        logger.Error("PC 電源オフコマンドの起動に失敗しました（プロセスが null）。");
        return false;
      }

      var stdout = process.StandardOutput.ReadToEnd();
      var stderr = process.StandardError.ReadToEnd();
      if (!process.WaitForExit(10_000))
      {
        logger.Error("PC 電源オフコマンドが応答しませんでした。");
        return false;
      }

      if (!IsSuccessfulExitCode(process.ExitCode))
      {
        var output = JoinOutput(stdout, stderr);
        var detail = string.IsNullOrEmpty(output) ? "" : $" {output}";
        logger.Error(
          $"PC 電源オフコマンドが失敗しました（exit={process.ExitCode}, shutdown {arguments}）。{detail}");
        return false;
      }

      if (delaySeconds == 0)
      {
        logger.Info($"PC 電源オフコマンドを実行しました（shutdown {arguments}）。");
      }
      else
      {
        logger.Info($"PC 電源オフを {delaySeconds} 秒後に予約しました（shutdown {arguments}）。");
      }

      return true;
    }
    catch (Exception ex)
    {
      logger.Error("PC 電源オフコマンドの実行に失敗しました。", ex);
      return false;
    }
  }

  internal static string BuildArguments(int delaySeconds) =>
    $"/s /t {Math.Max(0, delaySeconds)} /f";

  private static bool IsSuccessfulExitCode(int exitCode) =>
    exitCode is 0 or ErrorShutdownInProgress or ErrorShutdownIsScheduled;

  private static string JoinOutput(string stdout, string stderr)
  {
    var parts = new[] { stdout, stderr }
      .Select(value => value.Trim())
      .Where(value => value.Length > 0);
    return string.Join(" ", parts);
  }
}
