using System.Windows;
using System.Windows.Interop;
using PdfSignage.Interop;

namespace PdfSignage.Services;

/// <summary>
/// キオスク表示の制御（枠なし全画面・タスクバー非表示・Alt+Tab 緩和）。
/// <para>
/// Alt+Tab の完全抑制は Windows のセキュリティ制約により不可。
/// WS_EX_TOOLWINDOW による Alt+Tab リスト非表示と Topmost で可能な範囲を対応する。
/// </para>
/// </summary>
public sealed class KioskModeService : IDisposable
{
  private bool _altTabMitigationApplied;

  /// <summary>キオスクモードを有効化する。</summary>
  public void Activate(Window window)
  {
    ConfigureWindow(window);
    ApplyAltTabMitigation(window);
    TaskbarController.Suppress();
    window.Topmost = true;
    window.WindowState = WindowState.Maximized;
    window.Activate();
  }

  /// <summary>キオスクモードを無効化する（管理モード切替時など）。</summary>
  public void Deactivate(Window window)
  {
    window.Topmost = false;
    TaskbarController.Release();
  }

  public void Dispose()
  {
    TaskbarController.ForceRestore();
  }

  private static void ConfigureWindow(Window window)
  {
    window.WindowStyle = WindowStyle.None;
    window.ResizeMode = ResizeMode.NoResize;
    window.WindowStartupLocation = WindowStartupLocation.Manual;
    window.Left = 0;
    window.Top = 0;
    window.Width = SystemParameters.PrimaryScreenWidth;
    window.Height = SystemParameters.PrimaryScreenHeight;
  }

  private void ApplyAltTabMitigation(Window window)
  {
    if (_altTabMitigationApplied)
    {
      return;
    }

    void OnSourceInitialized(object? sender, EventArgs e)
    {
      window.SourceInitialized -= OnSourceInitialized;
      var helper = new WindowInteropHelper(window);
      if (helper.Handle == IntPtr.Zero)
      {
        return;
      }

      var exStyle = Win32Native.GetWindowLongPtr(helper.Handle, Win32Native.GwlExstyle);
      var newStyle = new IntPtr(exStyle.ToInt64() | Win32Native.WsExToolWindow);
      Win32Native.SetWindowLongPtr(helper.Handle, Win32Native.GwlExstyle, newStyle);
      _altTabMitigationApplied = true;
    }

    if (window.IsLoaded)
    {
      OnSourceInitialized(window, EventArgs.Empty);
      return;
    }

    window.SourceInitialized += OnSourceInitialized;
  }
}
