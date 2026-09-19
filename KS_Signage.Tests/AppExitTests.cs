using System.IO;
using KS_Signage.Services;

namespace KS_Signage.Tests;

public class AppExitTests
{
  [Fact]
  public void 電源オフのあとにプロセス終了する()
  {
    WithLogger((logger, logDir) =>
    {
      var sequence = new List<string>();

      AppExit.Execute(
        logger,
        "管理モード",
        () => sequence.Add("exit"),
        _ =>
        {
          sequence.Add("power");
          return true;
        });

      Assert.Equal(new[] { "power", "exit" }, sequence);
      Assert.Contains("アプリを終了し、PC を電源オフします（管理モード）", ReadLog(logDir));
    });
  }

  [Fact]
  public void 電源オフが失敗してもプロセスは終了する()
  {
    WithLogger((logger, logDir) =>
    {
      var exited = false;

      AppExit.Execute(
        logger,
        "スケジュール",
        () => exited = true,
        _ => false);

      Assert.True(exited);
      var log = ReadLog(logDir);
      Assert.Contains("アプリを終了し、PC を電源オフします（スケジュール）", log);
      Assert.Contains("PC 電源オフに失敗しました。アプリのみ終了します。", log);
    });
  }

  [Fact]
  public void 電源オフが例外でもプロセスは終了する()
  {
    WithLogger((logger, logDir) =>
    {
      var exited = false;

      AppExit.Execute(
        logger,
        "管理モード",
        () => exited = true,
        _ => throw new InvalidOperationException("denied"));

      Assert.True(exited);
      var log = ReadLog(logDir);
      Assert.Contains("PC 電源オフコマンドの実行に失敗しました。", log);
      Assert.Contains("PC 電源オフに失敗しました。アプリのみ終了します。", log);
    });
  }

  [Fact]
  public void アプリのみ終了では電源オフしない()
  {
    WithLogger((logger, logDir) =>
    {
      var exited = false;

      AppExit.ExitProcess(logger, "管理モード", () => exited = true);

      Assert.True(exited);
      var log = ReadLog(logDir);
      Assert.Contains("アプリを終了します（管理モード）。PC は電源オフしません。", log);
      Assert.DoesNotContain("電源オフします", log);
    });
  }

  [Fact]
  public void アプリのみ終了でも理由が空なら実行しない()
  {
    WithLogger((logger, _) =>
    {
      Assert.Throws<ArgumentException>(() =>
        AppExit.ExitProcess(logger, "  ", () => { }));
    });
  }

  [Fact]
  public void 理由が空なら実行しない()
  {
    WithLogger((logger, _) =>
    {
      Assert.Throws<ArgumentException>(() =>
        AppExit.Execute(logger, "  ", () => { }, _ => true));
    });
  }

  [Fact]
  public void 終了アクションがnullなら実行しない()
  {
    WithLogger((logger, _) =>
    {
      Assert.Throws<ArgumentNullException>(() =>
        AppExit.Execute(logger, "管理モード", null!, _ => true));
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
