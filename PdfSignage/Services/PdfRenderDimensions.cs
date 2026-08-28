using Docnet.Core.Models;

namespace PdfSignage.Services;

/// <summary>
/// Docnet（PDFium）向けのレンダリング寸法を計算するヘルパー。
/// </summary>
public static class PdfRenderDimensions
{
  /// <summary>
  /// 画面サイズから Docnet の <see cref="PageDimensions"/> を生成する。
  /// <para>
  /// Docnet の制約: dimOne は dimTwo 以下である必要がある（dimOne &lt;= dimTwo）。
  /// 横長ディスプレイ（例: 1920×1080）では width &gt; height のため、
  /// そのまま (width, height) を渡すと例外が発生する。
  /// そのため小さい方を dimOne、大きい方を dimTwo として渡す。
  /// </para>
  /// </summary>
  /// <param name="screenWidth">画面幅（ピクセル）</param>
  /// <param name="screenHeight">画面高さ（ピクセル）</param>
  public static PageDimensions Create(int screenWidth, int screenHeight)
  {
    var dimOne = Math.Min(screenWidth, screenHeight);
    var dimTwo = Math.Max(screenWidth, screenHeight);
    return new PageDimensions(dimOne, dimTwo);
  }
}
