using PdfSignage.Services;

namespace PdfSignage.Tests;

public class NaturalStringComparerTests
{
  private static List<string> Sort(params string[] names)
  {
    var list = names.ToList();
    list.Sort(NaturalStringComparer.Instance);
    return list;
  }

  [Fact]
  public void 桁揃えされたファイル名を昇順に並べる()
  {
    Assert.Equal(
      ["001.jpg", "002.jpg", "010.jpg"],
      Sort("010.jpg", "001.jpg", "002.jpg"));
  }

  [Fact]
  public void 桁が揃っていなくても数値として比較する()
  {
    // 辞書順なら 1, 10, 2 になるところを 1, 2, 10 にする
    Assert.Equal(
      ["1.jpg", "2.jpg", "10.jpg"],
      Sort("10.jpg", "1.jpg", "2.jpg"));
  }

  [Fact]
  public void 先頭の0詰めは数値としては同値だが文字列長で決着する()
  {
    // 数値部分は 7 と 7 で同値。決着がつかないため最後に文字列長を比較し、
    // 短い "7.jpg" が先に来る（同名衝突時の順序を安定させるための挙動）
    Assert.True(NaturalStringComparer.Instance.Compare("007.jpg", "7.jpg") > 0);
  }

  [Fact]
  public void 大文字小文字を区別しない()
  {
    Assert.Equal(0, NaturalStringComparer.Instance.Compare("Photo.JPG", "photo.jpg"));
  }

  [Fact]
  public void 数字と文字が混在しても順序が安定する()
  {
    Assert.Equal(
      ["001_会社方針.pdf", "002_安全目標.pdf", "010_案内.mp4"],
      Sort("010_案内.mp4", "002_安全目標.pdf", "001_会社方針.pdf"));
  }

  [Fact]
  public void nullは先頭に来る()
  {
    Assert.True(NaturalStringComparer.Instance.Compare(null, "a") < 0);
    Assert.True(NaturalStringComparer.Instance.Compare("a", null) > 0);
    Assert.Equal(0, NaturalStringComparer.Instance.Compare(null, null));
  }
}
