namespace PdfSignage.Models;

/// <summary>
/// サイネージの表示単位（スライド）を表すモデル。
/// <para>
/// 画像ファイルは 1 ファイルが 1 スライド。
/// PDF は各ページが個別のスライドとして展開される。
/// 動画は 1 ファイルが 1 スライド（表示秒数は使用しない）。
/// </para>
/// </summary>
public sealed class Slide
{
  /// <summary>
  /// スライドを生成する。
  /// </summary>
  /// <param name="contentType">コンテンツ種別（画像 / PDF ページ / 動画）</param>
  /// <param name="filePath">元ファイルのフルパス</param>
  /// <param name="pageIndex">PDF のページ番号（0 始まり）。画像の場合は常に 0</param>
  /// <param name="displaySeconds">このスライドの表示秒数（動画は未使用）</param>
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

  /// <summary>コンテンツ種別（画像 / PDF ページ / 動画）</summary>
  public SlideContentType ContentType { get; }

  /// <summary>元ファイルのフルパス</summary>
  public string FilePath { get; }

  /// <summary>
  /// PDF のページ番号（0 始まり）。
  /// 画像スライドの場合は常に 0。
  /// </summary>
  public int PageIndex { get; }

  /// <summary>
  /// 表示秒数（秒）。画像・PDF ページのタイマー切替に使用。動画は再生完了で進行するため未使用。
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
