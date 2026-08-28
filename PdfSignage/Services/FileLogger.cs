namespace PdfSignage.Services;

public class FileLogger
{
  private string _logDirectory;
  private readonly object _lock = new();

  public FileLogger(string logDirectory)
  {
    _logDirectory = logDirectory;
    Directory.CreateDirectory(_logDirectory);
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
      var filePath = Path.Combine(_logDirectory, $"app_{DateTime.Now:yyyyMMdd}.log");
      File.AppendAllText(filePath, line + Environment.NewLine);
    }
  }
}
