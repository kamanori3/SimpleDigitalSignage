using System.IO;
using PdfSignage.Services;

namespace PdfSignage.Tests;

public class SystemPowerTests
{
  [Fact]
  public void 起動情報は即時電源オフである()
  {
    var startInfo = SystemPower.CreateShutdownStartInfo();
    Assert.Equal("shutdown", startInfo.FileName);
    Assert.Equal("/s /t 0", startInfo.Arguments);
    Assert.True(startInfo.CreateNoWindow);
    Assert.False(startInfo.UseShellExecute);
    Assert.Equal(SystemPower.ShutdownFileName, startInfo.FileName);
    Assert.Equal(SystemPower.ShutdownArguments, startInfo.Arguments);
  }

  [Fact]
  public void 成功したらtrueを返しログに残す()
  {
    WithLogger((logger, logDir) =>
    {
      Assert.True(SystemPower.TryPowerOff(logger, () => true));
      Assert.Contains("PC 電源オフコマンドを実行しました（shutdown /s /t 0）", ReadLog(logDir));
    });
  }

  [Fact]
  public void 起動失敗はfalseでログに残す()
  {
    WithLogger((logger, logDir) =>
    {
      Assert.False(SystemPower.TryPowerOff(logger, () => false));
      Assert.Contains("PC 電源オフコマンドの起動に失敗しました。", ReadLog(logDir));
    });
  }

  [Fact]
  public void 例外はfalseでログに残す()
  {
    WithLogger((logger, logDir) =>
    {
      Assert.False(SystemPower.TryPowerOff(logger, () => throw new InvalidOperationException("denied")));
      var log = ReadLog(logDir);
      Assert.Contains("PC 電源オフコマンドの実行に失敗しました。", log);
      Assert.Contains("denied", log);
    });
  }

  [Fact]
  public void テスト用コールバックを渡せば実プロセスは起動しない()
  {
    WithLogger((logger, _) =>
    {
      var called = false;
      Assert.True(SystemPower.TryPowerOff(logger, () =>
      {
        called = true;
        return true;
      }));
      Assert.True(called);
    });
  }

  private static void WithLogger(Action<FileLogger, string> test)
  {
    var logDir = Path.Combine(Path.GetTempPath(), "PdfSignageTests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(logDir);
    try
    {
      test(new FileLogger(logDir), logDir);
    }
    finally
    {
      try
      {
        Directory.Delete(logDir, recursive: true);
      }
      catch
      {
        // 一時フォルダの掃除に失敗してもテスト自体は落とさない
      }
    }
  }

  private static string ReadLog(string logDir)
  {
    var files = Directory.GetFiles(logDir, "app_*.log");
    Assert.NotEmpty(files);
    return File.ReadAllText(files[0]);
  }
}
