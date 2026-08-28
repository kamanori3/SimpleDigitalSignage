using PdfSignage.Models;

namespace PdfSignage.Services;

/// <summary>
/// 監視フォルダのファイル一覧から、表示用スライドリスト（プレイリスト）を構築する。
/// <para>
/// 変換ルール:
/// - 画像ファイル → 1 スライド（ファイル名 <c>_秒数</c> で表示時間を上書き可）
/// - PDF ファイル → ページ数分のスライド（全ページ共通の表示時間）
/// - 動画ファイル → 1 スライド（再生完了まで表示、秒数は無視）
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
  /// <param name="renderWidth">PDF レンダラー初期化用（キャッシュキーとして PdfRenderer に渡済み）</param>
  /// <param name="renderHeight">PDF レンダラー初期化用</param>
  /// <returns>表示順のスライドリスト</returns>
  public IReadOnlyList<Slide> Build(string folderPath, int renderWidth, int renderHeight)
  {
    var contentFiles = ContentFolderScanner.Scan(folderPath);
    var slides = new List<Slide>();

    foreach (var filePath in contentFiles)
    {
      if (ContentFolderScanner.IsImageFile(filePath))
      {
        var displaySeconds = ResolveDisplaySeconds(filePath);
        slides.Add(new Slide(
          SlideContentType.Image,
          filePath,
          pageIndex: 0,
          displaySeconds: displaySeconds));
        continue;
      }

      if (ContentFolderScanner.IsPdfFile(filePath))
      {
        var displaySeconds = ResolveDisplaySeconds(filePath);

        try
        {
          var pageCount = _pdfRenderer.GetPageCount(filePath);

          for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
          {
            slides.Add(new Slide(
              SlideContentType.PdfPage,
              filePath,
              pageIndex: pageIndex,
              displaySeconds: displaySeconds));
          }

          _logger.Info(
            $"PDF 読込: {Path.GetFileName(filePath)} ({pageCount} ページ, {displaySeconds} 秒/ページ)");
        }
        catch (Exception ex)
        {
          _logger.Error($"PDF 読込失敗（スキップ）: {filePath} - {ex.Message}");
        }

        continue;
      }

      if (ContentFolderScanner.IsVideoFile(filePath))
      {
        slides.Add(new Slide(
          SlideContentType.Video,
          filePath,
          pageIndex: 0,
          displaySeconds: 0));
        _logger.Info($"動画読込: {Path.GetFileName(filePath)}（再生完了まで表示）");
      }
    }

    return slides;
  }

  private int ResolveDisplaySeconds(string filePath)
  {
    var seconds = DisplayDurationParser.Resolve(filePath, _defaultDisplaySeconds);

    if (DisplayDurationParser.HasCustomDuration(filePath))
    {
      _logger.Info($"表示秒数: {Path.GetFileName(filePath)} → {seconds} 秒");
    }

    return seconds;
  }
}
