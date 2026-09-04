using PdfSignage.Services;

namespace PdfSignage.Tests;

public class DriveFolderUrlParserTests
{
  [Theory]
  [InlineData("https://drive.google.com/drive/folders/abcdefghijklmnopqrstuvwx", "abcdefghijklmnopqrstuvwx")]
  [InlineData("https://drive.google.com/drive/folders/abcdefghijklmnopqrstuvwx?usp=sharing", "abcdefghijklmnopqrstuvwx")]
  [InlineData("https://drive.google.com/drive/u/0/folders/abcdefghijklmnopqrstuvwx", "abcdefghijklmnopqrstuvwx")]
  [InlineData("https://drive.google.com/folderview?id=abcdefghijklmnopqrstuvwx", "abcdefghijklmnopqrstuvwx")]
  [InlineData("abcdefghijklmnopqrstuvwx", "abcdefghijklmnopqrstuvwx")]
  public void フォルダURLからIDを取り出せる(string input, string expected)
  {
    Assert.True(DriveFolderUrlParser.TryParseFolderId(input, out var id));
    Assert.Equal(expected, id);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("https://drive.google.com/file/d/abcdefghijklmnopqrstuvwx/view")]
  [InlineData("https://example.com/folders/abc")]
  public void ファイルリンクや空は拒否する(string input)
  {
    Assert.False(DriveFolderUrlParser.TryParseFolderId(input, out _));
  }
}
