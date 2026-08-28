namespace PdfSignage.Models;

/// <summary>
/// アプリケーション設定（settings.json に永続化）
/// </summary>
public class AppSettings
{
    public const int MinDisplaySeconds = 5;
    public const int MaxDisplaySeconds = 300;
    public const int DefaultDisplaySecondsValue = 15;

    public string WatchFolderPath { get; set; } = "D:\\Signage";

    public int DefaultDisplaySeconds { get; set; } = DefaultDisplaySecondsValue;

    public bool WindowsAutoStart { get; set; } = false;

    /// <summary>日次アプリ終了時刻（HH:mm）。未設定時は null。</summary>
    public string? AppExitTime { get; set; }

    /// <summary>日次 PC 電源オフ時刻（HH:mm）。未設定時は null。</summary>
    public string? PcShutdownTime { get; set; }

    public string RecoveryMessage { get; set; } = "表示を復旧しています";
}
