namespace KS_Signage.Issuer;

/// <summary>
/// 秘密鍵と発行台帳の場所。リポジトリルートの <c>secrets/</c> を探す。
/// </summary>
public static class IssuerPaths
{
  public const string SecretsDirectoryName = "secrets";
  public const string PrivateFileName = "access-key-private.pem";
  public const string PublicFileName = "access-key-public.pem";
  public const string LedgerFileName = "issued-access-keys.json";

  public static string ResolveSecretsDirectory()
  {
    foreach (var start in UniqueStarts())
    {
      foreach (var dir in WalkUp(start))
      {
        var secrets = Path.Combine(dir, SecretsDirectoryName);
        if (File.Exists(Path.Combine(secrets, PrivateFileName)))
        {
          return Path.GetFullPath(secrets);
        }

        if (File.Exists(Path.Combine(dir, "KS_Signage.sln")))
        {
          return Path.GetFullPath(Path.Combine(dir, SecretsDirectoryName));
        }
      }
    }

    return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), SecretsDirectoryName));
  }

  private static IEnumerable<string> UniqueStarts()
  {
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
      var full = Path.GetFullPath(start);
      if (seen.Add(full))
      {
        yield return full;
      }
    }
  }

  private static IEnumerable<string> WalkUp(string start)
  {
    var current = new DirectoryInfo(Path.GetFullPath(start));
    while (current is not null)
    {
      yield return current.FullName;
      current = current.Parent;
    }
  }
}
