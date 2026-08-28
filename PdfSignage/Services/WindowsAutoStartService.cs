using System.Diagnostics;
using Microsoft.Win32;

namespace PdfSignage.Services;

/// <summary>
/// Windows 起動時の自動起動設定（レジストリ Run キー）。
/// </summary>
public static class WindowsAutoStartService
{
  private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
  private const string RegistryValueName = "PdfSignage";

  public static void Apply(bool enabled, FileLogger logger)
  {
    try
    {
      using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
      if (key is null)
      {
        logger.Error("Windows 自動起動: レジストリキーを開けませんでした。");
        return;
      }

      if (enabled)
      {
        var exePath = GetExecutablePath();
        if (string.IsNullOrWhiteSpace(exePath))
        {
          logger.Error("Windows 自動起動: 実行ファイルパスを取得できませんでした。");
          return;
        }

        key.SetValue(RegistryValueName, $"\"{exePath}\"");
        logger.Info($"Windows 自動起動: ON（レジストリ Run に登録: {exePath}）");
        return;
      }

      if (key.GetValue(RegistryValueName) is not null)
      {
        key.DeleteValue(RegistryValueName, throwOnMissingValue: false);
      }

      logger.Info("Windows 自動起動: OFF（レジストリ Run から削除）");
    }
    catch (Exception ex)
    {
      logger.Error("Windows 自動起動のレジストリ更新に失敗しました。", ex);
    }
  }

  private static string? GetExecutablePath()
  {
    var exePath = Environment.ProcessPath;
    if (!string.IsNullOrWhiteSpace(exePath))
    {
      return exePath;
    }

    return Process.GetCurrentProcess().MainModule?.FileName;
  }
}
