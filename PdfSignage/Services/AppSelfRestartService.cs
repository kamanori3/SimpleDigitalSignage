using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace PdfSignage.Services;

/// <summary>
/// 未処理例外時の自己再起動と、通常起動時の二重起動防止を担当する。
/// </summary>
public static class AppSelfRestartService
{
  private const string RestartEnvVar = "PDFSIGNAGE_SELF_RESTART";
  private const string MutexName = "Global\\PdfSignage.SingleInstance";
  private static readonly TimeSpan RestartCooldown = TimeSpan.FromSeconds(30);

  private static Mutex? _instanceMutex;
  private static bool _handlersRegistered;
  private static int _restartInProgress;

  public static bool IsSelfRestartProcess()
  {
    return Environment.GetEnvironmentVariable(RestartEnvVar) is not null;
  }

  /// <summary>
  /// 単一インスタンスのロックを取得する。既に起動中なら false。
  /// </summary>
  public static bool TryAcquireSingleInstance()
  {
    _instanceMutex = new Mutex(true, MutexName, out var createdNew);
    if (!createdNew)
    {
      _instanceMutex.Dispose();
      _instanceMutex = null;
      return false;
    }

    return true;
  }

  public static void ReleaseSingleInstance()
  {
    if (_instanceMutex is null)
    {
      return;
    }

    try
    {
      _instanceMutex.ReleaseMutex();
    }
    catch (ApplicationException)
    {
      // このスレッドがロックを保持していない場合は無視
    }

    _instanceMutex.Dispose();
    _instanceMutex = null;
  }

  public static void RegisterExceptionHandlers(FileLogger logger)
  {
    if (_handlersRegistered)
    {
      return;
    }

    _handlersRegistered = true;

    if (Application.Current is not null)
    {
      Application.Current.DispatcherUnhandledException += (_, e) =>
      {
        e.Handled = true;
        HandleFatalError(e.Exception, logger, "DispatcherUnhandledException");
      };
    }

    AppDomain.CurrentDomain.UnhandledException += (_, e) =>
    {
      if (e.ExceptionObject is Exception ex)
      {
        HandleFatalError(ex, logger, "AppDomain.UnhandledException");
      }
    };

    TaskScheduler.UnobservedTaskException += (_, e) =>
    {
      e.SetObserved();
      HandleFatalError(e.Exception, logger, "UnobservedTaskException");
    };
  }

  private static void HandleFatalError(Exception ex, FileLogger logger, string source)
  {
    logger.Error($"{source}: {ex.Message}");
    logger.Error(ex.ToString());

    if (!ShouldAttemptRestart())
    {
      logger.Error("自己再起動をスキップしました（再起動直後の再クラッシュを検知）。");
      return;
    }

    Restart(logger);
  }

  private static bool ShouldAttemptRestart()
  {
    var marker = Environment.GetEnvironmentVariable(RestartEnvVar);
    if (marker is null)
    {
      return true;
    }

    if (!long.TryParse(marker, out var ticks))
    {
      return true;
    }

    var restartTime = new DateTime(ticks, DateTimeKind.Utc);
    return DateTime.UtcNow - restartTime >= RestartCooldown;
  }

  public static void Restart(FileLogger logger)
  {
    if (Interlocked.Exchange(ref _restartInProgress, 1) == 1)
    {
      return;
    }

    var exePath = Environment.ProcessPath;
    if (string.IsNullOrWhiteSpace(exePath))
    {
      exePath = Process.GetCurrentProcess().MainModule?.FileName;
    }

    if (string.IsNullOrWhiteSpace(exePath))
    {
      logger.Error("自己再起動に失敗: 実行ファイルパスを取得できませんでした。");
      return;
    }

    logger.Error("未処理例外により自己再起動を実行します。");

    ReleaseSingleInstance();

    var startInfo = new ProcessStartInfo
    {
      FileName = exePath,
      UseShellExecute = false,
      WorkingDirectory = AppContext.BaseDirectory
    };
    startInfo.Environment[RestartEnvVar] = DateTime.UtcNow.Ticks.ToString();

    try
    {
      Process.Start(startInfo);
    }
    catch (Exception ex)
    {
      logger.Error($"自己再起動に失敗: {ex.Message}");
      return;
    }

    if (Application.Current?.Dispatcher is Dispatcher dispatcher && !dispatcher.CheckAccess())
    {
      dispatcher.Invoke(() => Application.Current.Shutdown());
      return;
    }

    Application.Current?.Shutdown();
  }
}
