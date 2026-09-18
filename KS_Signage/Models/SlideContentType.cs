namespace KS_Signage.Models;

/// <summary>
/// スライド（表示単位）のコンテンツ種別。
/// </summary>
public enum SlideContentType
{
  /// <summary>JPEG 等の静止画ファイル（1 ファイル = 1 スライド）</summary>
  Image,

  /// <summary>PDF の 1 ページ（1 ページ = 1 スライド）</summary>
  PdfPage,

  /// <summary>MP4 動画ファイル（1 ファイル = 1 スライド、再生完了まで表示）</summary>
  Video
}
