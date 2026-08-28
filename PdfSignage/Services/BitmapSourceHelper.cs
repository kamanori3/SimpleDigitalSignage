using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PdfSignage.Services;

/// <summary>
/// バイト列から WPF の ImageSource を生成するヘルパー。
/// Docnet（PDFium）が出力する BGRA 形式のピクセルデータを WPF で表示可能な形式に変換する。
/// </summary>
public static class BitmapSourceHelper
{
  /// <summary>
  /// BGRA32 形式のピクセルデータから <see cref="BitmapSource"/> を生成する。
  /// </summary>
  /// <param name="bgraPixels">BGRA 順のピクセルバイト列（Docnet の GetImage() 出力）</param>
  /// <param name="width">画像幅（ピクセル）</param>
  /// <param name="height">画像高さ（ピクセル）</param>
  /// <returns>Freeze 済みの BitmapSource（UI スレッド外からの参照も安全）</returns>
  public static BitmapSource FromBgra32(byte[] bgraPixels, int width, int height)
  {
    if (width <= 0 || height <= 0)
    {
      throw new ArgumentException("画像サイズが不正です。", nameof(width));
    }

    // BGRA は 1 ピクセル 4 バイト
    var expectedLength = width * height * 4;
    if (bgraPixels.Length < expectedLength)
    {
      throw new ArgumentException(
        $"ピクセルデータの長さが不足しています。期待: {expectedLength}, 実際: {bgraPixels.Length}",
        nameof(bgraPixels));
    }

    var stride = width * 4;
    var bitmap = BitmapSource.Create(
      width,
      height,
      96,
      96,
      PixelFormats.Bgra32,
      null,
      bgraPixels,
      stride);

    // Freeze することでスレッド間での共有可能化・パフォーマンス向上
    bitmap.Freeze();
    return bitmap;
  }
}
