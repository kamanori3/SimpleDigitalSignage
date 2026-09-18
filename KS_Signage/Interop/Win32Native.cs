using System.Runtime.InteropServices;

namespace KS_Signage.Interop;

/// <summary>
/// コンテンツ表示モード用 Win32 API（タスクバー制御・ウィンドウスタイル）。
/// </summary>
internal static class Win32Native
{
  public const int GwlExstyle = -20;
  public const int WsExToolWindow = 0x00000080;
  public const int SwHide = 0;
  public const int SwShow = 5;
  public const int SwRestore = 9;
  public const uint AbmSetState = 0x0000000a;
  public const string TaskbarClassName = "Shell_TrayWnd";
  public const string SecondaryTaskbarClassName = "Shell_SecondaryTrayWnd";

  [StructLayout(LayoutKind.Sequential)]
  public struct Rect
  {
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct AppBarData
  {
    public int cbSize;
    public IntPtr hWnd;
    public uint uCallbackMessage;
    public uint uEdge;
    public Rect rc;
    public int lParam;
  }

  [DllImport("user32.dll", CharSet = CharSet.Unicode)]
  public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

  [DllImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

  [DllImport("shell32.dll")]
  public static extern uint SHAppBarMessage(uint dwMessage, ref AppBarData pData);

  [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
  private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

  [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
  private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

  [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
  private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

  [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
  private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

  public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex) =>
    IntPtr.Size == 8
      ? GetWindowLongPtr64(hWnd, nIndex)
      : new IntPtr(GetWindowLong32(hWnd, nIndex));

  public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong) =>
    IntPtr.Size == 8
      ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
      : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
}
