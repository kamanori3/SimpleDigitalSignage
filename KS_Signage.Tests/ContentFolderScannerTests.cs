using KS_Signage.Services;

namespace KS_Signage.Tests;

/// <summary>
/// 拡張子判定のみを対象とする（ファイル列挙は実 I/O のため受け入れテストでカバー）。
/// </summary>
public class ContentFolderScannerTests
{
  [Theory]
  [InlineData("a.jpg", true)]
  [InlineData("a.jpeg", true)]
  [InlineData("a.JPG", true)]
  [InlineData("a.png", false)]
  [InlineData("a.pdf", false)]
  public void 画像判定(string name, bool expected)
  {
    Assert.Equal(expected, ContentFolderScanner.IsImageFile(name));
  }

  [Theory]
  [InlineData("a.pdf", true)]
  [InlineData("a.PDF", true)]
  [InlineData("a.jpg", false)]
  public void PDF判定(string name, bool expected)
  {
    Assert.Equal(expected, ContentFolderScanner.IsPdfFile(name));
  }

  [Theory]
  [InlineData("a.mp4", true)]
  [InlineData("a.MP4", true)]
  [InlineData("a.mov", false)]
  [InlineData("a.wmv", false)]
  public void 動画判定(string name, bool expected)
  {
    Assert.Equal(expected, ContentFolderScanner.IsVideoFile(name));
  }

  [Theory]
  [InlineData(@"D:\Signage\001.jpg", true)]
  [InlineData(@"D:\Signage\002.pdf", true)]
  [InlineData(@"D:\Signage\003.mp4", true)]
  [InlineData(@"D:\Signage\readme.txt", false)]
  [InlineData(@"D:\Signage\photo.png", false)]
  [InlineData(@"D:\Signage\clip.mov", false)]
  public void 対応形式はJPEG_PDF_MP4のみ(string path, bool expected)
  {
    Assert.Equal(expected, ContentFolderScanner.IsContentFile(path));
  }

  [Fact]
  public void 存在しないフォルダは空を返す()
  {
    var missing = System.IO.Path.Combine(
      System.IO.Path.GetTempPath(),
      $"pdfsignage-missing-{Guid.NewGuid():N}");
    Assert.Empty(ContentFolderScanner.Scan(missing));
  }
}
