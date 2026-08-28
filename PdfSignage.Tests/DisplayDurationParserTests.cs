using PdfSignage.Services;

namespace PdfSignage.Tests;

public class DisplayDurationParserTests
{
  private const int Default = 15;

  [Theory]
  [InlineData(@"D:\Signage\003_イベント案内_120.pdf", 120)]
  [InlineData(@"D:\Signage\ガイダンス_50.jpeg", 50)]
  [InlineData(@"D:\Signage\写真_30.jpg", 30)]
  public void 拡張子直前の秒数を採用する(string path, int expected)
  {
    Assert.Equal(expected, DisplayDurationParser.Resolve(path, Default));
  }

  [Theory]
  [InlineData(@"D:\Signage\案内.pdf")]
  [InlineData(@"D:\Signage\001_会社方針.pdf")]
  public void 秒数指定がなければデフォルトを使う(string path)
  {
    Assert.Equal(Default, DisplayDurationParser.Resolve(path, Default));
  }

  [Theory]
  [InlineData(@"D:\Signage\案内_4.pdf")]    // 下限 5 未満
  [InlineData(@"D:\Signage\案内_301.pdf")]  // 上限 300 超過
  [InlineData(@"D:\Signage\IMG_2716.jpg")]  // 秒数のつもりではない末尾数値
  public void 範囲外の数値はデフォルトへフォールバックする(string path)
  {
    Assert.Equal(Default, DisplayDurationParser.Resolve(path, Default));
  }

  [Theory]
  [InlineData(@"D:\Signage\案内_5.pdf", 5)]
  [InlineData(@"D:\Signage\案内_300.pdf", 300)]
  public void 境界値は有効として扱う(string path, int expected)
  {
    Assert.Equal(expected, DisplayDurationParser.Resolve(path, Default));
  }

  [Fact]
  public void 途中にあるアンダースコア数値は対象外()
  {
    // 末尾が「_数字」でない限り採用しない
    Assert.Equal(Default, DisplayDurationParser.Resolve(@"D:\Signage\120_案内.pdf", Default));
  }

  [Theory]
  [InlineData(@"D:\Signage\案内_120.pdf", true)]
  [InlineData(@"D:\Signage\案内.pdf", false)]
  [InlineData(@"D:\Signage\IMG_2716.jpg", false)]
  public void HasCustomDurationは有効な指定のみtrueを返す(string path, bool expected)
  {
    Assert.Equal(expected, DisplayDurationParser.HasCustomDuration(path));
  }
}
