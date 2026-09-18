namespace KS_Signage.Services;

/// <summary>
/// 監視フォルダ直下のコンテンツファイルをスキャンする（JPEG + PDF + MP4）。
/// <para>
/// - サブフォルダは対象外（要件 3.4）
/// - ファイル名の自然順でソート（001, 002, 010 の順）
/// </para>
/// </summary>
public static class ContentFolderScanner
{
  /// <summary>サポートする画像拡張子</summary>
  private static readonly string[] ImageExtensions = [".jpg", ".jpeg"];

  /// <summary>サポートする PDF 拡張子</summary>
  private static readonly string[] PdfExtensions = [".pdf"];

  /// <summary>サポートする動画拡張子</summary>
  private static readonly string[] VideoExtensions = [".mp4"];

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

  /// <summary>動画ファイルかどうかを拡張子で判定</summary>
  public static bool IsVideoFile(string filePath)
  {
    var extension = Path.GetExtension(filePath);
    return VideoExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>表示対象のコンテンツファイルかどうかを判定</summary>
  public static bool IsContentFile(string filePath)
  {
    return IsSupportedContent(filePath);
  }

  private static bool IsSupportedContent(string filePath)
  {
    return IsImageFile(filePath) || IsPdfFile(filePath) || IsVideoFile(filePath);
  }
}
