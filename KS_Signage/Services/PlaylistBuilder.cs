using KS_Signage.Models;

namespace KS_Signage.Services;

/// <summary>
/// 監視フォルダのファイル一覧から、表示用スライドリスト（プレイリスト）を構築する。
/// <para>
/// 変換ルール:
/// - 画像ファイル → 1 スライド（ファイル名 <c>_秒数</c> で表示時間を上書き可）
/// - PDF ファイル → ページ数分のスライド（全ページ共通の表示時間）
/// - 動画ファイル → 1 スライド（再生完了まで表示、秒数は無視）
/// - 再生期限切れ（<c>today &gt; expiresOn</c>）はスライド化しない
/// </para>
/// <para>
/// ファイル名末尾の秒数・期限は <see cref="DisplayDurationParser"/> が解析する。
/// 入力誤りがあってもスライドは作り、WARN ログだけ残す（ADR 0009）。
/// 管理モード入場時の MessageBox は <c>MainWindow</c> 側の責務。
/// </para>
/// </summary>
public sealed class PlaylistBuilder
{
  private readonly PdfRenderer _pdfRenderer;
  private readonly FileLogger _logger;

  /// <summary>ファイル名に有効な秒数がないときの表示秒数（管理画面のデフォルト）。</summary>
  private readonly int _defaultDisplaySeconds;

  /// <summary>
  /// プレイリスト構築器を生成する。
  /// </summary>
  /// <param name="pdfRenderer">PDF のページ数取得に使用</param>
  /// <param name="logger">読込結果・期限切れ・ファイル名誤りのログ出力先</param>
  /// <param name="defaultDisplaySeconds">秒数指定がない画像・PDF に使う表示秒数</param>
  public PlaylistBuilder(PdfRenderer pdfRenderer, FileLogger logger, int defaultDisplaySeconds)
  {
    _pdfRenderer = pdfRenderer;
    _logger = logger;
    _defaultDisplaySeconds = defaultDisplaySeconds;
  }

  /// <summary>
  /// フォルダ内の全コンテンツをスライド列に展開する。
  /// 自然順でスキャンし、期限切れは除外、種別ごとに <see cref="Slide"/> を追加する。
  /// </summary>
  /// <param name="folderPath">監視フォルダ</param>
  /// <param name="renderWidth">PDF レンダラー初期化用（キャッシュキーとして PdfRenderer に渡済み）</param>
  /// <param name="renderHeight">PDF レンダラー初期化用</param>
  /// <param name="today">期限判定に使う日付。省略時はシステムの今日</param>
  /// <returns>表示順のスライドリスト（期限切れ・読込失敗分は含まれない）</returns>
  public IReadOnlyList<Slide> Build(
    string folderPath,
    int renderWidth,
    int renderHeight,
    DateOnly? today = null)
  {
    // テストから日付を差し込めるよう、省略時だけ実日付を使う
    var effectiveToday = today ?? DateOnly.FromDateTime(DateTime.Today);
    var contentFiles = ContentFolderScanner.Scan(folderPath);
    var slides = new List<Slide>();

    foreach (var filePath in contentFiles)
    {
      // 期限切れはここで落とす。ファイル名誤りは false にせず、秒数だけフォールバックする
      if (!TryResolvePlayback(filePath, effectiveToday, out var displaySeconds))
      {
        continue;
      }

      if (ContentFolderScanner.IsImageFile(filePath))
      {
        slides.Add(new Slide(
          SlideContentType.Image,
          filePath,
          pageIndex: 0,
          displaySeconds: displaySeconds));
        continue;
      }

      if (ContentFolderScanner.IsPdfFile(filePath))
      {
        try
        {
          var pageCount = _pdfRenderer.GetPageCount(filePath);

          // 1 PDF = ページ数分のスライド。表示秒数は全ページ共通
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
          // 壊れた PDF はスキップし、他のコンテンツは続ける
          _logger.Error($"PDF 読込失敗（スキップ）: {filePath}", ex);
        }

        continue;
      }

      if (ContentFolderScanner.IsVideoFile(filePath))
      {
        // 表示秒数は使わない（ADR 0004）。進行は MediaEnded。期限判定は上で済んでいる
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

  /// <summary>
  /// ファイル名から表示秒数を決め、プレイリストに載せるかを判定する。
  /// </summary>
  /// <returns>
  /// 期限切れなら <c>false</c>（スライド化しない）。
  /// 入力誤りがあっても <c>true</c>（誤った指定だけ捨てて再生を続ける）。
  /// </returns>
  private bool TryResolvePlayback(string filePath, DateOnly today, out int displaySeconds)
  {
    var parsed = DisplayDurationParser.Parse(filePath);
    var fileName = Path.GetFileName(filePath);

    // 管理モードの MessageBox 用エラーと同じ内容をログにも残す
    foreach (var error in parsed.Errors)
    {
      _logger.Warn($"ファイル名の指定誤り: {fileName} — {error}");
    }

    // 指定日当日までは再生。翌日以降が期限切れ（today > expiresOn）
    if (parsed.ExpiresOn is { } expiresOn && DisplayDurationParser.IsExpired(expiresOn, today))
    {
      _logger.Info($"再生期限切れ（スキップ）: {fileName}（期限 {expiresOn:yyyyMMdd}）");
      displaySeconds = 0;
      return false;
    }

    displaySeconds = parsed.CustomSeconds ?? _defaultDisplaySeconds;
    if (parsed.CustomSeconds is not null)
    {
      _logger.Info($"表示秒数: {fileName} → {displaySeconds} 秒");
    }

    return true;
  }
}
