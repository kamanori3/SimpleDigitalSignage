using PdfSignage.Services;

namespace PdfSignage.Tests;

/// <summary>
/// ログフォルダの算出のみを対象とする。
/// ResolveWatchFolderPath / ResolveLogDirectory はフォルダを作成する副作用があるため、
/// ここでは扱わず受け入れテストでカバーする。
/// </summary>
public class PathHelperTests
{
  [Fact]
  public void ログフォルダは監視フォルダの親階層に作る()
  {
    Assert.Equal(@"D:\logs", PathHelper.GetLogDirectory(@"D:\Signage"));
  }

  [Fact]
  public void 入れ子のフォルダでも親階層に作る()
  {
    Assert.Equal(@"C:\app\data\logs", PathHelper.GetLogDirectory(@"C:\app\data\signage"));
  }

  [Fact]
  public void 末尾の区切り文字があっても同じ結果になる()
  {
    Assert.Equal(
      PathHelper.GetLogDirectory(@"D:\Signage"),
      PathHelper.GetLogDirectory(@"D:\Signage\"));
  }

  [Fact]
  public void ドライブ直下を指定した場合は自身の下に作る()
  {
    // 親が存在しないため、監視フォルダ自身の直下へフォールバックする
    Assert.Equal(@"D:\logs", PathHelper.GetLogDirectory(@"D:\"));
  }

  [Fact]
  public void 開発用フォルダは実行ファイルの横()
  {
    Assert.Equal(
      System.IO.Path.Combine(AppContext.BaseDirectory, "SignageData"),
      PathHelper.GetDevSignageDataPath());
  }
}
