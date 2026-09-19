using Docnet.Core;
using System.Windows.Media;

namespace KS_Signage.Services;

/// <summary>
/// PDF ファイルのページ読込・ラスタライズを担当するサービス。
/// <para>
/// パフォーマンス対策:
/// - 同一 PDF の IDocReader をキャッシュし、ページ切替時の再オープンを回避
/// - プレイリスト構築時にドキュメントを開き、表示中は保持する
/// </para>
/// </summary>
public sealed class PdfRenderer : IDisposable
{
  private static readonly DocLib DocLibInstance = DocLib.Instance;

  private readonly FileLogger _logger;
  private readonly int _renderWidth;
  private readonly int _renderHeight;
  private readonly Dictionary<string, CachedPdfDocument> _documentCache = new();
  private readonly object _cacheLock = new();

  public PdfRenderer(FileLogger logger, int renderWidth, int renderHeight)
  {
    _logger = logger;
    _renderWidth = renderWidth;
    _renderHeight = renderHeight;
  }

  /// <summary>
  /// PDF の総ページ数を取得する。初回アクセス時にドキュメントをキャッシュへ登録する。
  /// </summary>
  public int GetPageCount(string pdfFilePath)
  {
    return GetOrOpenDocument(pdfFilePath).PageCount;
  }

  /// <summary>
  /// 指定ページをビットマップにレンダリングする（白背景合成済み）。
  /// </summary>
  public ImageSource RenderPage(string pdfFilePath, int pageIndex)
  {
    if (!File.Exists(pdfFilePath))
    {
      throw new FileNotFoundException("PDF ファイルが見つかりません。", pdfFilePath);
    }

    var document = GetOrOpenDocument(pdfFilePath);
    return document.RenderPage(pageIndex, _logger);
  }

  /// <summary>
  /// キャッシュ済みドキュメントを取得する。未登録なら新規オープンしてキャッシュする。
  /// </summary>
  private CachedPdfDocument GetOrOpenDocument(string pdfFilePath)
  {
    lock (_cacheLock)
    {
      if (_documentCache.TryGetValue(pdfFilePath, out var cached))
      {
        return cached;
      }

      _logger.Info($"PDF ドキュメントをオープン: {Path.GetFileName(pdfFilePath)}");
      cached = new CachedPdfDocument(pdfFilePath, _renderWidth, _renderHeight);
      _documentCache[pdfFilePath] = cached;
      return cached;
    }
  }

  /// <summary>
  /// プレイリスト再構築時などにキャッシュを解放する。
  /// </summary>
  public void ClearCache()
  {
    lock (_cacheLock)
    {
      foreach (var document in _documentCache.Values)
      {
        document.Dispose();
      }

      _documentCache.Clear();
    }
  }

  public void Dispose()
  {
    ClearCache();
  }
}
