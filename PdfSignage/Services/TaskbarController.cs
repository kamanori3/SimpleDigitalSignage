using System.Runtime.InteropServices;
using PdfSignage.Interop;

namespace PdfSignage.Services;

/// <summary>
/// Windows タスクバーの表示・非表示を制御する。
/// プロセス異常終了時にも <see cref="ForceRestore"/> で復元できるよう参照カウントを管理する。
/// </summary>
public static class TaskbarController
{
  private static int _suppressCount;

  /// <summary>タスクバーを非表示にする（初回のみ Win32 呼び出し）。</summary>
  public static void Suppress()
  {
    if (_suppressCount == 0)
    {
      SetVisibility(visible: false);
    }

    _suppressCount++;
  }

  /// <summary>タスクバー非表示の参照を 1 つ解放する。</summary>
  public static void Release()
  {
    if (_suppressCount <= 0)
    {
      ForceRestore();
      return;
    }

    _suppressCount--;
    if (_suppressCount == 0)
    {
      SetVisibility(visible: true);
    }
  }

  /// <summary>タスクバーを強制的に復元する（アプリ終了時など）。</summary>
  public static void ForceRestore()
  {
    _suppressCount = 0;
    SetVisibility(visible: true);
  }

  private static void SetVisibility(bool visible)
  {
    if (visible)
    {
      RestoreTaskbar(Win32Native.TaskbarClassName);
      RestoreTaskbar(Win32Native.SecondaryTaskbarClassName);
      return;
    }

    HideTaskbar(Win32Native.TaskbarClassName);
    HideTaskbar(Win32Native.SecondaryTaskbarClassName);
  }

  private static void HideTaskbar(string className)
  {
    var handle = Win32Native.FindWindow(className, null);
    if (handle == IntPtr.Zero)
    {
      return;
    }

    Win32Native.ShowWindow(handle, Win32Native.SwHide);
  }

  private static void RestoreTaskbar(string className)
  {
    var handle = Win32Native.FindWindow(className, null);
    if (handle == IntPtr.Zero)
    {
      return;
    }

    // Windows 11 では ShowWindow だけでは復元されないことがあるため ABM_SETSTATE も併用する。
    var appBarData = new Win32Native.AppBarData
    {
      cbSize = Marshal.SizeOf<Win32Native.AppBarData>(),
      hWnd = handle,
      lParam = 0
    };
    Win32Native.SHAppBarMessage(Win32Native.AbmSetState, ref appBarData);

    Win32Native.ShowWindow(handle, Win32Native.SwRestore);
    Win32Native.ShowWindow(handle, Win32Native.SwShow);
  }
}
