namespace PdfSignage.Services;

/// <summary>
/// Windows 起動時の自動起動設定（Phase 9 でレジストリ連携予定。Phase 7 では UI と settings.json のみ）。
/// </summary>
public static class WindowsAutoStartService
{
  public static void Apply(bool enabled, FileLogger logger)
  {
    logger.Info(
      enabled
        ? "Windows 自動起動: ON（settings.json に保存。レジストリ連携は Phase 9 で実装予定）"
        : "Windows 自動起動: OFF（settings.json に保存。レジストリ連携は Phase 9 で実装予定）");
  }
}
