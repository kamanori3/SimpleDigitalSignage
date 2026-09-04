using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using PdfSignage.Models;

namespace PdfSignage.Services;

/// <summary>
/// 公開 Google Drive フォルダ直下の JPEG / PDF / MP4 をローカルキャッシュへ同期する。
/// </summary>
public sealed class DriveFolderSyncService : IDisposable
{
  private readonly FileLogger _logger;
  private readonly HttpClient _httpClient;
  private readonly string _cacheDirectory;
  private readonly string _apiKey;
  private readonly string _folderId;
  private readonly TimeSpan _interval;
  private readonly CancellationTokenSource _cts = new();
  private Task? _loopTask;
  private bool _disposed;

  public DriveFolderSyncService(
    FileLogger logger,
    string cacheDirectory,
    string apiKey,
    string folderId,
    int intervalMinutes)
  {
    _logger = logger;
    _cacheDirectory = cacheDirectory;
    _apiKey = apiKey;
    _folderId = folderId;
    _interval = TimeSpan.FromMinutes(Math.Clamp(
      intervalMinutes,
      AppSettings.MinGoogleDriveSyncIntervalMinutes,
      AppSettings.MaxGoogleDriveSyncIntervalMinutes));

    _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
    _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PdfSignage/0.1");
  }

  public void Start()
  {
    Directory.CreateDirectory(_cacheDirectory);
    _loopTask = RunLoopAsync(_cts.Token);
  }

  public void Dispose()
  {
    if (_disposed)
    {
      return;
    }

    _disposed = true;
    _cts.Cancel();
    try
    {
      _loopTask?.Wait(TimeSpan.FromSeconds(3));
    }
    catch (OperationCanceledException)
    {
    }
    catch (Exception ex)
    {
      _logger.Error($"Drive 同期の停止中にエラー: {ex.Message}");
    }

    _httpClient.Dispose();
    _cts.Dispose();
  }

  private async Task RunLoopAsync(CancellationToken cancellationToken)
  {
    await SyncSafeAsync(cancellationToken);
    using var timer = new PeriodicTimer(_interval);
    try
    {
      while (await timer.WaitForNextTickAsync(cancellationToken))
      {
        await SyncSafeAsync(cancellationToken);
      }
    }
    catch (OperationCanceledException)
    {
    }
  }

  private async Task SyncSafeAsync(CancellationToken cancellationToken)
  {
    try
    {
      var changed = await SyncOnceAsync(cancellationToken);
      if (changed)
      {
        _logger.Info("Google Drive の同期でキャッシュを更新しました。");
      }
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (Exception ex)
    {
      _logger.Error($"Google Drive 同期に失敗しました（前回キャッシュで継続）: {ex.Message}");
    }
  }

  internal async Task<bool> SyncOnceAsync(CancellationToken cancellationToken)
  {
    var remoteFiles = await ListRemoteFilesAsync(cancellationToken);
    var changed = false;
    var keepNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var file in remoteFiles)
    {
      if (string.IsNullOrEmpty(file.Id) || string.IsNullOrEmpty(file.Name) || !ContentFolderScanner.IsContentFile(file.Name))
      {
        continue;
      }

      var localName = Disambiguate(ToSafeFileName(file.Name), keepNames, file.Id);
      keepNames.Add(localName);

      var localPath = Path.Combine(_cacheDirectory, localName);
      if (File.Exists(localPath) && !NeedsDownload(localPath, file))
      {
        continue;
      }

      await DownloadFileAsync(file.Id, localPath, cancellationToken);
      ApplyTimestamp(localPath, file.ModifiedTime);
      changed = true;
      _logger.Info($"Drive から取得: {localName}");
    }

    foreach (var existing in Directory.EnumerateFiles(_cacheDirectory))
    {
      var name = Path.GetFileName(existing);
      if (!ContentFolderScanner.IsContentFile(name) || keepNames.Contains(name))
      {
        continue;
      }

      try
      {
        File.Delete(existing);
        changed = true;
        _logger.Info($"Drive 上に無いためキャッシュから削除: {name}");
      }
      catch (IOException ex)
      {
        _logger.Error($"キャッシュ削除に失敗（次回再試行）: {name} - {ex.Message}");
      }
    }

    return changed;
  }

  private async Task<List<DriveFileDto>> ListRemoteFilesAsync(CancellationToken cancellationToken)
  {
    var results = new List<DriveFileDto>();
    string? pageToken = null;
    var query = $"'{_folderId}' in parents and trashed = false";

    do
    {
      var url =
        "https://www.googleapis.com/drive/v3/files"
        + "?q=" + Uri.EscapeDataString(query)
        + "&fields=" + Uri.EscapeDataString("nextPageToken,files(id,name,mimeType,md5Checksum,modifiedTime)")
        + "&pageSize=1000"
        + "&key=" + Uri.EscapeDataString(_apiKey);
      if (!string.IsNullOrEmpty(pageToken))
      {
        url += "&pageToken=" + Uri.EscapeDataString(pageToken);
      }

      using var response = await _httpClient.GetAsync(url, cancellationToken);
      var body = await response.Content.ReadAsStringAsync(cancellationToken);
      if (!response.IsSuccessStatusCode)
      {
        throw new InvalidOperationException(
          $"Drive API 一覧が失敗しました ({(int)response.StatusCode})。フォルダが「リンクを知っている全員が閲覧可」か確認してください。");
      }

      var parsed = JsonSerializer.Deserialize<DriveFileListDto>(body)
                   ?? throw new InvalidOperationException("Drive API の応答を解析できませんでした。");
      if (parsed.Files is not null)
      {
        results.AddRange(parsed.Files);
      }

      pageToken = parsed.NextPageToken;
    }
    while (!string.IsNullOrEmpty(pageToken));

    return results;
  }

  private async Task DownloadFileAsync(string fileId, string localPath, CancellationToken cancellationToken)
  {
    var url =
      "https://www.googleapis.com/drive/v3/files/"
      + Uri.EscapeDataString(fileId)
      + "?alt=media&key="
      + Uri.EscapeDataString(_apiKey);

    var tempPath = localPath + ".tmp";
    try
    {
      using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
      if (!response.IsSuccessStatusCode)
      {
        throw new InvalidOperationException($"Drive からのダウンロードに失敗しました ({(int)response.StatusCode}): {Path.GetFileName(localPath)}");
      }

      await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
      await using (var output = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
      {
        await input.CopyToAsync(output, cancellationToken);
      }

      File.Move(tempPath, localPath, overwrite: true);
    }
    finally
    {
      if (File.Exists(tempPath))
      {
        try
        {
          File.Delete(tempPath);
        }
        catch
        {
          // 一時ファイルの削除失敗は次回同期で上書きされる
        }
      }
    }
  }

  private static bool NeedsDownload(string localPath, DriveFileDto remote)
  {
    if (!string.IsNullOrEmpty(remote.Md5Checksum))
    {
      try
      {
        using var stream = File.OpenRead(localPath);
        var hash = Convert.ToHexString(System.Security.Cryptography.MD5.HashData(stream));
        return !hash.Equals(remote.Md5Checksum, StringComparison.OrdinalIgnoreCase);
      }
      catch
      {
        return true;
      }
    }

    if (DateTimeOffset.TryParse(remote.ModifiedTime, out var remoteModified))
    {
      var localWrite = File.GetLastWriteTimeUtc(localPath);
      return remoteModified.UtcDateTime > localWrite + TimeSpan.FromSeconds(2);
    }

    return true;
  }

  private static void ApplyTimestamp(string localPath, string? modifiedTime)
  {
    if (DateTimeOffset.TryParse(modifiedTime, out var remoteModified))
    {
      try
      {
        File.SetLastWriteTimeUtc(localPath, remoteModified.UtcDateTime);
      }
      catch
      {
        // 時刻を合わせられなくても次回 md5 で判定できる
      }
    }
  }

  internal static string ToSafeFileName(string name)
  {
    var invalid = Path.GetInvalidFileNameChars();
    var safe = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
    return string.IsNullOrEmpty(safe) ? "unnamed" : safe;
  }

  private static string Disambiguate(string localName, HashSet<string> used, string fileId)
  {
    if (!used.Contains(localName))
    {
      return localName;
    }

    var stem = Path.GetFileNameWithoutExtension(localName);
    var ext = Path.GetExtension(localName);
    var suffix = fileId.Length >= 8 ? fileId[..8] : fileId;
    return $"{stem}_{suffix}{ext}";
  }

  private sealed class DriveFileListDto
  {
    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; set; }

    [JsonPropertyName("files")]
    public List<DriveFileDto>? Files { get; set; }
  }

  private sealed class DriveFileDto
  {
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }

    [JsonPropertyName("md5Checksum")]
    public string? Md5Checksum { get; set; }

    [JsonPropertyName("modifiedTime")]
    public string? ModifiedTime { get; set; }
  }
}
