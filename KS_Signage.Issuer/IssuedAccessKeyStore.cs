using System.Text.Json;
using System.Text.Json.Serialization;

namespace KS_Signage.Issuer;

/// <summary>
/// 発行済みアクセスキーの JSON 台帳。<c>secrets/</c> に置き、git 対象外。
/// </summary>
public sealed class IssuedAccessKeyStore
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  private readonly string _path;

  public IssuedAccessKeyStore(string path)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(path);
    _path = path;
  }

  public string Path => _path;

  public IReadOnlyList<IssuedAccessKeyRecord> Load()
  {
    if (!File.Exists(_path))
    {
      return [];
    }

    var json = File.ReadAllText(_path);
    if (string.IsNullOrWhiteSpace(json))
    {
      return [];
    }

    var records = JsonSerializer.Deserialize<List<IssuedAccessKeyRecord>>(json, JsonOptions);
    return records ?? [];
  }

  public void Append(IssuedAccessKeyRecord record)
  {
    ArgumentNullException.ThrowIfNull(record);
    var directory = System.IO.Path.GetDirectoryName(_path);
    if (!string.IsNullOrEmpty(directory))
    {
      Directory.CreateDirectory(directory);
    }

    var records = Load().ToList();
    records.Add(record);
    var tmp = _path + ".tmp";
    File.WriteAllText(tmp, JsonSerializer.Serialize(records, JsonOptions));
    File.Move(tmp, _path, overwrite: true);
  }
}
