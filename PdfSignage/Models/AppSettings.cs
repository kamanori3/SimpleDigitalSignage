namespace PdfSignage.Models;

/// <summary>
/// アプリケーション設定（settings.json に永続化）
/// </summary>
public class AppSettings
{
  public const int MinDisplaySeconds = 5;
  public const int MaxDisplaySeconds = 300;
  public const int DefaultDisplaySecondsValue = 15;

  public const int MinGoogleDriveSyncIntervalMinutes = 1;
  public const int MaxGoogleDriveSyncIntervalMinutes = 60;
  public const int DefaultGoogleDriveSyncIntervalMinutes = 5;

  public string WatchFolderPath { get; set; } = "D:\\Signage";

  /// <summary>
  /// Google Drive フォルダの共有 URL。空ならローカル監視フォルダを使う。
  /// 指定時は公開フォルダ（リンクを知っている全員が閲覧可）が必要。
  /// </summary>
  public string GoogleDriveFolderUrl { get; set; } = "";

  /// <summary>Drive API キー。フォルダ URL 指定時は必須。</summary>
  public string GoogleDriveApiKey { get; set; } = "";

  /// <summary>Drive からの同期間隔（分）。</summary>
  public int GoogleDriveSyncIntervalMinutes { get; set; } = DefaultGoogleDriveSyncIntervalMinutes;

  public bool UsesGoogleDrive => !string.IsNullOrWhiteSpace(GoogleDriveFolderUrl);

  public int DefaultDisplaySeconds { get; set; } = DefaultDisplaySecondsValue;

  public bool WindowsAutoStart { get; set; } = false;

  /// <summary>日次アプリ終了時刻（HH:mm）。未設定時は null。</summary>
  public string? AppExitTime { get; set; }

  /// <summary>日次 PC 電源オフ時刻（HH:mm）。未設定時は null。</summary>
  public string? PcShutdownTime { get; set; }

  public string RecoveryMessage { get; set; } = "表示を復旧しています";
}
