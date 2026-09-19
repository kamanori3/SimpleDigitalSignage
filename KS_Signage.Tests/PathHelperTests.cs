using KS_Signage.Services;

namespace KS_Signage.Tests;

/// <summary>
/// ログフォルダの固定パスと、ネットワークパス拒否時のフォールバックを対象とする。
/// ローカルフォルダを作成する ResolveWatchFolderPath の成功経路は受け入れテストでカバーする。
/// </summary>
public class PathHelperTests
{
  [Fact]
  public void ログフォルダはこのPCのLocalAppData()
  {
    var expected = System.IO.Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "KS_Signage",
      "logs");
    Assert.Equal(expected, PathHelper.GetLogDirectory());
  }

  [Fact]
  public void 開発用フォルダは実行ファイルの横()
  {
    Assert.Equal(
      System.IO.Path.Combine(AppContext.BaseDirectory, "SignageData"),
      PathHelper.GetDevSignageDataPath());
  }

  [Fact]
  public void 未許可のUNCは開発用フォルダへフォールバックする()
  {
    var resolved = PathHelper.ResolveWatchFolderPath(
      @"\\server\share\signage", allowNetworkWatchFolder: false);
    Assert.Equal(PathHelper.GetDevSignageDataPath(), resolved);
  }
}
