namespace PdfSignage.Services;

public class FileLogger
{
  private readonly string _logDirectory;
  private readonly object _lock = new();

  public FileLogger(string logDirectory)
  {
    _logDirectory = logDirectory;
    Directory.CreateDirectory(_logDirectory);
  }

  public string LogDirectory => _logDirectory;

  public void Info(string message)
  {
    Write("INFO", message);
  }

  public void Error(string message)
  {
    Write("ERROR", message);
  }

  public void Warn(string message)
  {
    Write("WARN", message);
  }

  private void Write(string level, string message)
  {
    var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
    var filePath = Path.Combine(_logDirectory, $"app_{DateTime.Now:yyyyMMdd}.log");

    lock (_lock)
    {
      File.AppendAllText(filePath, line + Environment.NewLine);
    }
  }
}
