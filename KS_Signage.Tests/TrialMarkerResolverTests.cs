using KS_Signage.Licensing;

namespace KS_Signage.Tests;

public class TrialMarkerResolverTests
{
  private static readonly DateOnly Today = new(2026, 9, 12);
  private static readonly DateOnly Earlier = new(2026, 7, 1);
  private static readonly DateOnly Later = new(2026, 8, 1);

  [Fact]
  public void どれも無ければ今日を採用しすべてへ書く()
  {
    var chosen = TrialMarkerResolver.Resolve([null, null, null, null], Today, out var writeFlags);
    Assert.Equal(Today, chosen);
    Assert.Equal([true, true, true, true], writeFlags);
  }

  [Fact]
  public void マシンレジストリだけあればそれを採用し他へ書く()
  {
    var chosen = TrialMarkerResolver.Resolve([Earlier, null, null, null], Today, out var writeFlags);
    Assert.Equal(Earlier, chosen);
    Assert.Equal([false, true, true, true], writeFlags);
  }

  [Fact]
  public void ユーザーファイルだけあればそれを採用し他へ書く()
  {
    var chosen = TrialMarkerResolver.Resolve([null, null, null, Earlier], Today, out var writeFlags);
    Assert.Equal(Earlier, chosen);
    Assert.Equal([true, true, true, false], writeFlags);
  }

  [Fact]
  public void 複数あり早い方を採用し遅い側と欠け側へ書き戻す()
  {
    var chosen = TrialMarkerResolver.Resolve([Later, null, Earlier, Later], Today, out var writeFlags);
    Assert.Equal(Earlier, chosen);
    Assert.Equal([true, true, false, true], writeFlags);
  }

  [Fact]
  public void すべて同じなら書かない()
  {
    var chosen = TrialMarkerResolver.Resolve([Earlier, Earlier, Earlier, Earlier], Today, out var writeFlags);
    Assert.Equal(Earlier, chosen);
    Assert.Equal([false, false, false, false], writeFlags);
  }

  [Fact]
  public void ストアが空なら例外()
  {
    Assert.Throws<ArgumentException>(() =>
      TrialMarkerResolver.Resolve(Array.Empty<DateOnly?>(), Today, out _));
  }
}
