using PdfSignage.Licensing;

namespace PdfSignage.Tests;

public class TrialMarkerResolverTests
{
  private static readonly DateOnly Today = new(2026, 9, 12);
  private static readonly DateOnly Earlier = new(2026, 7, 1);
  private static readonly DateOnly Later = new(2026, 8, 1);

  [Fact]
  public void どちらも無ければ今日を採用し両方へ書く()
  {
    var chosen = TrialMarkerResolver.Resolve(null, null, Today, out var writeRegistry, out var writeFile);
    Assert.Equal(Today, chosen);
    Assert.True(writeRegistry);
    Assert.True(writeFile);
  }

  [Fact]
  public void レジストリだけあればそれを採用しファイルへ書く()
  {
    var chosen = TrialMarkerResolver.Resolve(Earlier, null, Today, out var writeRegistry, out var writeFile);
    Assert.Equal(Earlier, chosen);
    Assert.False(writeRegistry);
    Assert.True(writeFile);
  }

  [Fact]
  public void ファイルだけあればそれを採用しレジストリへ書く()
  {
    var chosen = TrialMarkerResolver.Resolve(null, Earlier, Today, out var writeRegistry, out var writeFile);
    Assert.Equal(Earlier, chosen);
    Assert.True(writeRegistry);
    Assert.False(writeFile);
  }

  [Fact]
  public void 両方あり早い方を採用し遅い側へ書き戻す()
  {
    var chosen = TrialMarkerResolver.Resolve(Later, Earlier, Today, out var writeRegistry, out var writeFile);
    Assert.Equal(Earlier, chosen);
    Assert.True(writeRegistry);
    Assert.False(writeFile);
  }

  [Fact]
  public void 両方同じなら書かない()
  {
    var chosen = TrialMarkerResolver.Resolve(Earlier, Earlier, Today, out var writeRegistry, out var writeFile);
    Assert.Equal(Earlier, chosen);
    Assert.False(writeRegistry);
    Assert.False(writeFile);
  }
}
