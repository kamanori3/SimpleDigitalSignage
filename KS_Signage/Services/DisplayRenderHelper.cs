using System.Windows;

namespace KS_Signage.Services;

/// <summary>
/// ディスプレイの描画サイズを取得するヘルパー。
/// PDF のレンダリング解像度決定に使用する（画面いっぱいに表示するための基準サイズ）。
/// </summary>
public static class DisplayRenderHelper
{
  /// <summary>
  /// プライマリディスプレイの幅・高さ（ピクセル）を返す。
  /// シングルディスプレイ想定のため PrimaryScreen を使用。
  /// </summary>
  /// <returns>幅と高さのタプル（最低 640x480 を保証）</returns>
  public static (int Width, int Height) GetPrimaryScreenSize()
  {
    // SystemParameters は WPF の画面情報。起動直後でも利用可能。
    var width = (int)SystemParameters.PrimaryScreenWidth;
    var height = (int)SystemParameters.PrimaryScreenHeight;

    // 異常値のフォールバック（フル HD 相当）
    if (width < 640)
    {
      width = 1920;
    }

    if (height < 480)
    {
      height = 1080;
    }

    return (width, height);
  }
}
