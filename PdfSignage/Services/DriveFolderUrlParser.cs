using System.Text.RegularExpressions;

namespace PdfSignage.Services;

/// <summary>
/// Google Drive フォルダ URL からフォルダ ID を取り出す。
/// </summary>
public static partial class DriveFolderUrlParser
{
  [GeneratedRegex(@"drive\.google\.com/drive/(?:u/\d+/)?folders/([a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
  private static partial Regex FoldersPathPattern();

  [GeneratedRegex(@"drive\.google\.com/folderview\?.*?id=([a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
  private static partial Regex FolderViewPattern();

  [GeneratedRegex(@"^[a-zA-Z0-9_-]{20,}$", RegexOptions.CultureInvariant)]
  private static partial Regex BareIdPattern();

  public static bool TryParseFolderId(string? urlOrId, out string folderId)
  {
    folderId = "";
    if (string.IsNullOrWhiteSpace(urlOrId))
    {
      return false;
    }

    var trimmed = urlOrId.Trim();

    if (trimmed.Contains("/file/d/", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    var foldersMatch = FoldersPathPattern().Match(trimmed);
    if (foldersMatch.Success)
    {
      folderId = foldersMatch.Groups[1].Value;
      return true;
    }

    var viewMatch = FolderViewPattern().Match(trimmed);
    if (viewMatch.Success)
    {
      folderId = viewMatch.Groups[1].Value;
      return true;
    }

    if (BareIdPattern().IsMatch(trimmed) && !trimmed.Contains('/'))
    {
      folderId = trimmed;
      return true;
    }

    return false;
  }
}
