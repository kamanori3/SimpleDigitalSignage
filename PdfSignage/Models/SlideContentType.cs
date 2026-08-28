namespace PdfSignage.Models;

/// <summary>
/// スライド（表示単位）のコンテンツ種別。
/// Phase 2 では Image と PdfPage をサポート。Phase 3 で Video を追加予定。
/// </summary>
public enum SlideContentType
{
  /// <summary>JPEG 等の静止画ファイル（1 ファイル = 1 スライド）</summary>
  Image,

  /// <summary>PDF の 1 ページ（1 ページ = 1 スライド）</summary>
  PdfPage
}
