using System.Windows.Media.Imaging;

namespace KS_Signage.Services;

/// <summary>
/// 静止画ファイル（JPEG 等）を WPF の ImageSource に読み込むサービス。
/// <para>
/// URI ベースの BitmapImage を使用。CacheOption.OnLoad で
/// ファイル読込後に元ファイルへのロックを解放する。
/// </para>
/// </summary>
public static class ImageLoader
{
  /// <summary>
  /// 画像ファイルを読み込み、表示用 BitmapImage を返す。
  /// </summary>
  /// <param name="filePath">画像ファイルのフルパス</param>
  /// <returns>Freeze 済み BitmapImage</returns>
  public static BitmapImage Load(string filePath)
  {
    if (!File.Exists(filePath))
    {
      throw new FileNotFoundException("画像ファイルが見つかりません。", filePath);
    }

    var bitmap = new BitmapImage();
    bitmap.BeginInit();
    bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
    // OnLoad: EndInit 時にファイルを読み込み、以降はファイルロックを保持しない
    bitmap.CacheOption = BitmapCacheOption.OnLoad;
    bitmap.EndInit();
    bitmap.Freeze();
    return bitmap;
  }
}
