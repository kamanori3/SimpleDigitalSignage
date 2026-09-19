namespace KS_Signage.Services;

/// <summary>
/// 監視フォルダ直下のコンテンツファイル変更を検知する（Phase 5）。
/// <para>
/// 追加・削除・更新・リネームを検知し、<see cref="ContentChanged"/> を発火する。
/// サブフォルダは <see cref="FileSystemWatcher.IncludeSubdirectories"/> を false にして除外する。
/// </para>
/// </summary>
public sealed class ContentFolderWatcher : IDisposable
{
  private readonly FileSystemWatcher _watcher;
  private readonly FileLogger _logger;

  public ContentFolderWatcher(string folderPath, FileLogger logger)
  {
    _logger = logger;
    _watcher = new FileSystemWatcher(folderPath)
    {
      IncludeSubdirectories = false,
      NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
      EnableRaisingEvents = true
    };

    _watcher.Created += OnFileSystemEvent;
    _watcher.Deleted += OnFileSystemEvent;
    _watcher.Changed += OnFileSystemEvent;
    _watcher.Renamed += OnRenamed;
    _watcher.Error += OnError;
  }

  /// <summary>対象コンテンツの変更を検知したときに発火する。</summary>
  public event Action? ContentChanged;

  public void Dispose()
  {
    _watcher.EnableRaisingEvents = false;
    _watcher.Created -= OnFileSystemEvent;
    _watcher.Deleted -= OnFileSystemEvent;
    _watcher.Changed -= OnFileSystemEvent;
    _watcher.Renamed -= OnRenamed;
    _watcher.Error -= OnError;
    _watcher.Dispose();
  }

  private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
  {
    if (!IsRelevantContentChange(e.FullPath))
    {
      return;
    }

    _logger.Info($"フォルダ変更を検知: {e.ChangeType} - {Path.GetFileName(e.FullPath)}");
    ContentChanged?.Invoke();
  }

  private void OnRenamed(object sender, RenamedEventArgs e)
  {
    if (!IsRelevantContentChange(e.FullPath) && !IsRelevantContentChange(e.OldFullPath))
    {
      return;
    }

    _logger.Info(
      $"フォルダ変更を検知: Renamed - {Path.GetFileName(e.OldFullPath)} → {Path.GetFileName(e.FullPath)}");
    ContentChanged?.Invoke();
  }

  private void OnError(object sender, ErrorEventArgs e)
  {
    _logger.Error($"フォルダ監視エラー: {e.GetException().Message}");
  }

  private static bool IsRelevantContentChange(string path)
  {
    if (Directory.Exists(path))
    {
      return false;
    }

    return ContentFolderScanner.IsContentFile(path);
  }
}
