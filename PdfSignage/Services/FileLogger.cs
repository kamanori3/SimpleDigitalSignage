namespace PdfSignage.Services;

public class FileLogger
{
  public const int LogRetentionDays = 7;
  private const string LogFilePrefix = "app_";
  private const string LogFileExtension = ".log";

  private string _logDirectory;
  private readonly object _lock = new();

  public FileLogger(string logDirectory)
  {
    _logDirectory = logDirectory;
    Directory.CreateDirectory(_logDirectory);
    DeleteExpiredLogs();
  }

  public string LogDirectory
  {
    get
    {
      lock (_lock)
      {
        return _logDirectory;
      }
    }
  }

  /// <summary>
  /// 監視フォルダ変更時にログ出力先を更新する。
  /// </summary>
  public void SetLogDirectory(string logDirectory)
  {
    lock (_lock)
    {
      _logDirectory = logDirectory;
      Directory.CreateDirectory(_logDirectory);
      DeleteExpiredLogs();
    }
  }

  public void Info(string message)
  {
    Write("INFO", message);
  }

  public void Error(string message)
  {
    Write("ERROR", message);
  }

  public void Error(string message, Exception exception)
  {
    Write("ERROR", $"{message} - {exception.Message}");
    Write("ERROR", exception.ToString());
  }

  public void Warn(string message)
  {
    Write("WARN", message);
  }

  private void Write(string level, string message)
  {
    var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";

    lock (_lock)
    {
      var filePath = Path.Combine(_logDirectory, $"{LogFilePrefix}{DateTime.Now:yyyyMMdd}{LogFileExtension}");
      File.AppendAllText(filePath, line + Environment.NewLine);
    }
  }

  /// <summary>
  /// 保存期間（<see cref="LogRetentionDays"/> 日）を超えた日次ログを削除する。
  /// </summary>
  private void DeleteExpiredLogs()
  {
    if (!Directory.Exists(_logDirectory))
    {
      return;
    }

    var cutoffDate = DateTime.Today.AddDays(-LogRetentionDays);
    var deletedCount = 0;

    foreach (var filePath in Directory.EnumerateFiles(_logDirectory, $"{LogFilePrefix}*{LogFileExtension}"))
    {
      if (!TryGetLogFileDate(filePath, out var logDate) || logDate >= cutoffDate)
      {
        continue;
      }

      try
      {
        File.Delete(filePath);
        deletedCount++;
      }
      catch (Exception ex)
      {
        WriteWithoutRetentionCleanup(
          "WARN",
          $"古いログの削除に失敗: {Path.GetFileName(filePath)} - {ex.Message}");
      }
    }

    if (deletedCount > 0)
    {
      WriteWithoutRetentionCleanup(
        "INFO",
        $"保存期間（{LogRetentionDays} 日）を超えたログを {deletedCount} 件削除しました。");
    }
  }

  private static bool TryGetLogFileDate(string filePath, out DateTime logDate)
  {
    var fileName = Path.GetFileNameWithoutExtension(filePath);
    if (fileName.StartsWith(LogFilePrefix, StringComparison.Ordinal))
    {
      var datePart = fileName.Substring(LogFilePrefix.Length);
      if (DateTime.TryParseExact(
            datePart,
            "yyyyMMdd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out logDate))
      {
        return true;
      }
    }

    logDate = default;
    return false;
  }

  private void WriteWithoutRetentionCleanup(string level, string message)
  {
    var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
    var filePath = Path.Combine(_logDirectory, $"{LogFilePrefix}{DateTime.Now:yyyyMMdd}{LogFileExtension}");
    File.AppendAllText(filePath, line + Environment.NewLine);
  }
}
