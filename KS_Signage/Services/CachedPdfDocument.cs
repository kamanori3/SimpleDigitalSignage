using Docnet.Core;
using Docnet.Core.Readers;
using System.Windows.Media;

namespace KS_Signage.Services;

/// <summary>
/// 開いた PDF ドキュメントを保持し、ページのラスタライズを行う。
/// IDocReader を使い回すことで、ページ切替時のファイル再オープンを防ぐ。
/// </summary>
internal sealed class CachedPdfDocument : IDisposable
{
  private readonly IDocReader _documentReader;
  private readonly string _filePath;

  public CachedPdfDocument(string filePath, int renderWidth, int renderHeight)
  {
    _filePath = filePath;
    var dimensions = PdfRenderDimensions.Create(renderWidth, renderHeight);
    _documentReader = DocLib.Instance.GetDocReader(filePath, dimensions);
  }

  public string FilePath => _filePath;

  public int PageCount => _documentReader.GetPageCount();

  /// <summary>
  /// 指定ページを白背景合成済みの ImageSource として返す。
  /// </summary>
  public ImageSource RenderPage(int pageIndex, FileLogger logger)
  {
    if (pageIndex < 0 || pageIndex >= PageCount)
    {
      throw new ArgumentOutOfRangeException(
        nameof(pageIndex),
        $"ページ番号が範囲外です。index={pageIndex}, 総ページ数={PageCount}");
    }

    using var pageReader = _documentReader.GetPageReader(pageIndex);
    var pixelData = pageReader.GetImage();
    var pageWidth = pageReader.GetPageWidth();
    var pageHeight = pageReader.GetPageHeight();
    var visiblePixels = BitmapSourceHelper.CountVisiblePixels(pixelData);
    var totalPixels = pageWidth * pageHeight;

    // 空白ページ検出: 可視ピクセルが極端に少ない場合はログに警告
    if (totalPixels > 0 && visiblePixels < totalPixels * 0.001)
    {
      logger.Error(
        $"PDF ページがほぼ空白です: {Path.GetFileName(_filePath)} (ページ {pageIndex + 1}) " +
        $"可視ピクセル={visiblePixels}/{totalPixels}。PDF の再出力を検討してください。");
    }

    return BitmapSourceHelper.FlattenBgraOnWhite(pixelData, pageWidth, pageHeight);
  }

  public void Dispose()
  {
    _documentReader.Dispose();
  }
}
