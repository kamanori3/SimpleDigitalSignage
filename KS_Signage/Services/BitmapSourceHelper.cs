using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KS_Signage.Services;

/// <summary>
/// BGRA ピクセルデータを WPF の ImageSource に変換するヘルパー。
/// Docnet（PDFium）の出力は透明背景が多いため、白背景への合成も担当する。
/// </summary>
public static class BitmapSourceHelper
{
  /// <summary>
  /// BGRA32 形式のピクセルデータから <see cref="BitmapSource"/> を生成する。
  /// </summary>
  public static BitmapSource FromBgra32(byte[] bgraPixels, int width, int height)
  {
    ValidatePixelBuffer(bgraPixels, width, height);

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

    bitmap.Freeze();
    return bitmap;
  }

  /// <summary>
  /// 透明ピクセルを白背景に合成してから BitmapSource を生成する。
  /// <para>
  /// PDF は透明背景でレンダリングされることが多い。
  /// 黒画面のサイネージでは透明＝黒となり、内容が見えなくなるため白で塗りつぶす。
  /// </para>
  /// </summary>
  public static BitmapSource FlattenBgraOnWhite(byte[] bgraPixels, int width, int height)
  {
    ValidatePixelBuffer(bgraPixels, width, height);

    var flattened = new byte[bgraPixels.Length];
    var pixelCount = width * height;

    for (var i = 0; i < pixelCount; i++)
    {
      var index = i * 4;
      var blue = bgraPixels[index];
      var green = bgraPixels[index + 1];
      var red = bgraPixels[index + 2];
      var alpha = bgraPixels[index + 3];

      if (alpha == 255)
      {
        flattened[index] = blue;
        flattened[index + 1] = green;
        flattened[index + 2] = red;
        flattened[index + 3] = 255;
        continue;
      }

      if (alpha == 0)
      {
        flattened[index] = 255;
        flattened[index + 1] = 255;
        flattened[index + 2] = 255;
        flattened[index + 3] = 255;
        continue;
      }

      // 白背景への合成: result = src * α + white * (1 - α)
      var alphaFactor = alpha / 255f;
      flattened[index] = (byte)(blue * alphaFactor + 255 * (1 - alphaFactor));
      flattened[index + 1] = (byte)(green * alphaFactor + 255 * (1 - alphaFactor));
      flattened[index + 2] = (byte)(red * alphaFactor + 255 * (1 - alphaFactor));
      flattened[index + 3] = 255;
    }

    return FromBgra32(flattened, width, height);
  }

  /// <summary>
  /// ピクセルデータに実質的な描画内容があるか（空白ページ検出用）。
  /// アルファが 0 でないピクセル数を返す。
  /// </summary>
  public static int CountVisiblePixels(byte[] bgraPixels)
  {
    var visible = 0;
    for (var i = 3; i < bgraPixels.Length; i += 4)
    {
      if (bgraPixels[i] != 0)
      {
        visible++;
      }
    }

    return visible;
  }

  private static void ValidatePixelBuffer(byte[] bgraPixels, int width, int height)
  {
    if (width <= 0 || height <= 0)
    {
      throw new ArgumentException("画像サイズが不正です。", nameof(width));
    }

    var expectedLength = width * height * 4;
    if (bgraPixels.Length < expectedLength)
    {
      throw new ArgumentException(
        $"ピクセルデータの長さが不足しています。期待: {expectedLength}, 実際: {bgraPixels.Length}",
        nameof(bgraPixels));
    }
  }
}
