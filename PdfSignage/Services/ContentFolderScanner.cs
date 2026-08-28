namespace PdfSignage.Services;

/// <summary>
/// 監視フォルダ直下のコンテンツファイルをスキャンする（Phase 2: JPEG + PDF）。
/// <para>
/// - サブフォルダは対象外（要件 3.4）
/// - ファイル名の自然順でソート（001, 002, 010 の順）
/// </para>
/// </summary>
public static class ContentFolderScanner
{
  /// <summary>Phase 2 でサポートする画像拡張子</summary>
  private static readonly string[] ImageExtensions = [".jpg", ".jpeg"];

  /// <summary>Phase 2 でサポートする PDF 拡張子</summary>
  private static readonly string[] PdfExtensions = [".pdf"];

  /// <summary>
  /// 監視フォルダ直下の対象ファイルを自然順で取得する。
  /// </summary>
  /// <param name="folderPath">監視フォルダのフルパス</param>
  /// <returns>ファイルパスのリスト（ソート済み）</returns>
  public static IReadOnlyList<string> Scan(string folderPath)
  {
    if (!Directory.Exists(folderPath))
    {
      return Array.Empty<string>();
    }

    return Directory
      .EnumerateFiles(folderPath)
      .Where(IsSupportedContent)
      .OrderBy(file => Path.GetFileName(file), NaturalStringComparer.Instance)
      .ToList();
  }

  /// <summary>画像ファイルかどうかを拡張子で判定（大文字小文字を区別しない）</summary>
  public static bool IsImageFile(string filePath)
  {
    var extension = Path.GetExtension(filePath);
    return ImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>PDF ファイルかどうかを拡張子で判定</summary>
  public static bool IsPdfFile(string filePath)
  {
    var extension = Path.GetExtension(filePath);
    return PdfExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
  }

  private static bool IsSupportedContent(string filePath)
  {
    return IsImageFile(filePath) || IsPdfFile(filePath);
  }
}
