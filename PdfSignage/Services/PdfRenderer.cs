using Docnet.Core;
using System.Windows.Media;

namespace PdfSignage.Services;

/// <summary>
/// PDF ファイルのページ読込・ラスタライズ（ビットマップ化）を担当するサービス。
/// <para>
/// ライブラリ: Docnet.Core（PDFium の .NET ラッパー）
/// 各 PDF ページを画像に変換し、WPF の Image コントロールで表示する。
/// </para>
/// </summary>
public sealed class PdfRenderer
{
  /// <summary>
  /// Docnet のシングルトンインスタンス。ネイティブ PDFium ライブラリへの窓口。
  /// </summary>
  private static readonly DocLib DocLibInstance = DocLib.Instance;

  /// <summary>
  /// PDF の総ページ数を取得する（プレイリスト構築時に使用）。
  /// </summary>
  /// <param name="pdfFilePath">PDF ファイルのフルパス</param>
  /// <param name="renderWidth">レンダリング基準幅（画面幅に合わせる）</param>
  /// <param name="renderHeight">レンダリング基準高さ（画面高に合わせる）</param>
  /// <returns>ページ数（1 以上）</returns>
  public int GetPageCount(string pdfFilePath, int renderWidth, int renderHeight)
  {
    // PageDimensions は dimOne <= dimTwo の制約がある（横長画面では width,height の順では不可）
    var dimensions = PdfRenderDimensions.Create(renderWidth, renderHeight);

    using var docReader = DocLibInstance.GetDocReader(pdfFilePath, dimensions);
    return docReader.GetPageCount();
  }

  /// <summary>
  /// 指定ページをビットマップにレンダリングし、WPF 表示用の ImageSource を返す。
  /// </summary>
  /// <param name="pdfFilePath">PDF ファイルのフルパス</param>
  /// <param name="pageIndex">ページ番号（0 始まり）</param>
  /// <param name="renderWidth">レンダリング基準幅</param>
  /// <param name="renderHeight">レンダリング基準高さ</param>
  /// <returns>表示用 ImageSource（アスペクト比維持・画面フィット済みのラスタ画像）</returns>
  public ImageSource RenderPage(string pdfFilePath, int pageIndex, int renderWidth, int renderHeight)
  {
    if (!File.Exists(pdfFilePath))
    {
      throw new FileNotFoundException("PDF ファイルが見つかりません。", pdfFilePath);
    }

    var dimensions = PdfRenderDimensions.Create(renderWidth, renderHeight);

    // IDocReader: PDF ドキュメント全体。using でネイティブリソースを確実に解放。
    using var docReader = DocLibInstance.GetDocReader(pdfFilePath, dimensions);

    if (pageIndex < 0 || pageIndex >= docReader.GetPageCount())
    {
      throw new ArgumentOutOfRangeException(
        nameof(pageIndex),
        $"ページ番号が範囲外です。index={pageIndex}, 総ページ数={docReader.GetPageCount()}");
    }

    // IPageReader: 単一ページ。ピクセルデータの取得後は速やかに破棄する。
    using var pageReader = docReader.GetPageReader(pageIndex);

    // GetImage() は BGRA32 形式の生ピクセルデータを返す
    var pixelData = pageReader.GetImage();
    var pageWidth = pageReader.GetPageWidth();
    var pageHeight = pageReader.GetPageHeight();

    return BitmapSourceHelper.FromBgra32(pixelData, pageWidth, pageHeight);
  }
}
