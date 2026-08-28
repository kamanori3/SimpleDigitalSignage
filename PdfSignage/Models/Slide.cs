namespace PdfSignage.Models;

/// <summary>
/// サイネージの表示単位（スライド）を表すモデル。
/// <para>
/// 画像ファイルは 1 ファイルが 1 スライド。
/// PDF は各ページが個別のスライドとして展開される（Phase 4 でプレイリスト統合を強化）。
/// </para>
/// </summary>
public sealed class Slide
{
  /// <summary>
  /// スライドを生成する。
  /// </summary>
  /// <param name="contentType">コンテンツ種別（画像 or PDF ページ）</param>
  /// <param name="filePath">元ファイルのフルパス</param>
  /// <param name="pageIndex">PDF のページ番号（0 始まり）。画像の場合は常に 0</param>
  /// <param name="displaySeconds">このスライドの表示秒数（Phase 2 ではデフォルト値を使用）</param>
  public Slide(
    SlideContentType contentType,
    string filePath,
    int pageIndex,
    int displaySeconds)
  {
    ContentType = contentType;
    FilePath = filePath;
    PageIndex = pageIndex;
    DisplaySeconds = displaySeconds;
  }

  /// <summary>コンテンツ種別（画像 / PDF ページ）</summary>
  public SlideContentType ContentType { get; }

  /// <summary>元ファイルのフルパス</summary>
  public string FilePath { get; }

  /// <summary>
  /// PDF のページ番号（0 始まり）。
  /// 画像スライドの場合は常に 0。
  /// </summary>
  public int PageIndex { get; }

  /// <summary>
  /// 表示秒数（秒）。
  /// Phase 2 では設定のデフォルト値。Phase 4 でファイル名末尾の _秒数 を反映予定。
  /// </summary>
  public int DisplaySeconds { get; }

  /// <summary>ログ・デバッグ用の識別文字列</summary>
  public string GetDisplayName()
  {
    if (ContentType == SlideContentType.PdfPage)
    {
      return $"{Path.GetFileName(FilePath)} (ページ {PageIndex + 1})";
    }

    return Path.GetFileName(FilePath);
  }
}
