using PdfSignage.Models;

namespace PdfSignage.Services;

/// <summary>
/// 監視フォルダのファイル一覧から、表示用スライドリスト（プレイリスト）を構築する。
/// <para>
/// 変換ルール:
/// - 画像ファイル → 1 スライド
/// - PDF ファイル → ページ数分のスライド（各ページが 1 スライド）
/// </para>
/// </summary>
public sealed class PlaylistBuilder
{
  private readonly PdfRenderer _pdfRenderer;
  private readonly FileLogger _logger;
  private readonly int _defaultDisplaySeconds;

  public PlaylistBuilder(PdfRenderer pdfRenderer, FileLogger logger, int defaultDisplaySeconds)
  {
    _pdfRenderer = pdfRenderer;
    _logger = logger;
    _defaultDisplaySeconds = defaultDisplaySeconds;
  }

  /// <summary>
  /// フォルダ内の全コンテンツをスライド列に展開する。
  /// </summary>
  /// <param name="folderPath">監視フォルダ</param>
  /// <param name="renderWidth">PDF ページ数取得時のレンダリング基準幅</param>
  /// <param name="renderHeight">PDF ページ数取得時のレンダリング基準高さ</param>
  /// <returns>表示順のスライドリスト</returns>
  public IReadOnlyList<Slide> Build(string folderPath, int renderWidth, int renderHeight)
  {
    var contentFiles = ContentFolderScanner.Scan(folderPath);
    var slides = new List<Slide>();

    foreach (var filePath in contentFiles)
    {
      if (ContentFolderScanner.IsImageFile(filePath))
      {
        // 画像: 1 ファイル = 1 スライド
        slides.Add(new Slide(
          SlideContentType.Image,
          filePath,
          pageIndex: 0,
          displaySeconds: _defaultDisplaySeconds));
        continue;
      }

      if (ContentFolderScanner.IsPdfFile(filePath))
      {
        // PDF: 各ページを個別スライドに展開
        try
        {
          var pageCount = _pdfRenderer.GetPageCount(filePath, renderWidth, renderHeight);

          for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
          {
            slides.Add(new Slide(
              SlideContentType.PdfPage,
              filePath,
              pageIndex: pageIndex,
              displaySeconds: _defaultDisplaySeconds));
          }

          _logger.Info($"PDF 読込: {Path.GetFileName(filePath)} ({pageCount} ページ)");
        }
        catch (Exception ex)
        {
          // 破損 PDF 等はログに記録し、当該ファイルのみスキップ（要件 3.7）
          _logger.Error($"PDF 読込失敗（スキップ）: {filePath} - {ex.Message}");
        }
      }
    }

    return slides;
  }
}
