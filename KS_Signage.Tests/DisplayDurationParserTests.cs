using KS_Signage.Services;

namespace KS_Signage.Tests;

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

  [Fact]
  public void 秒数の後に期限があっても秒数を採用する()
  {
    var parsed = DisplayDurationParser.Parse(@"D:\Signage\案内_20_20260212.pdf");
    Assert.Equal(20, parsed.CustomSeconds);
    Assert.Equal(new DateOnly(2026, 2, 12), parsed.ExpiresOn);
    Assert.Empty(parsed.Errors);
  }

  [Fact]
  public void 期限の後に秒数があっても両方採用する()
  {
    var parsed = DisplayDurationParser.Parse(@"D:\Signage\案内_20260102_30.pdf");
    Assert.Equal(30, parsed.CustomSeconds);
    Assert.Equal(new DateOnly(2026, 1, 2), parsed.ExpiresOn);
    Assert.Empty(parsed.Errors);
  }

  [Fact]
  public void 期限のみなら秒数は無し()
  {
    var parsed = DisplayDurationParser.Parse(@"D:\Signage\案内_20260904.pdf");
    Assert.Null(parsed.CustomSeconds);
    Assert.Equal(new DateOnly(2026, 9, 4), parsed.ExpiresOn);
    Assert.Empty(parsed.Errors);
    Assert.Equal(Default, DisplayDurationParser.Resolve(@"D:\Signage\案内_20260904.pdf", Default));
  }

  [Fact]
  public void 存在しない日付は期限なしで誤りになる()
  {
    var parsed = DisplayDurationParser.Parse(@"D:\Signage\案内_20261301.pdf");
    Assert.Null(parsed.ExpiresOn);
    Assert.Null(parsed.CustomSeconds);
    Assert.Contains(parsed.Errors, error => error.Contains("20261301"));
  }

  [Fact]
  public void 範囲外秒数は誤りだが期限は残る()
  {
    var parsed = DisplayDurationParser.Parse(@"D:\Signage\写真_4_20260904.jpg");
    Assert.Null(parsed.CustomSeconds);
    Assert.Equal(new DateOnly(2026, 9, 4), parsed.ExpiresOn);
    Assert.Contains(parsed.Errors, error => error.Contains("4"));
  }

  [Fact]
  public void 秒数の重複はデフォルト秒数で誤りになる()
  {
    var parsed = DisplayDurationParser.Parse(@"D:\Signage\案内_20_30.pdf");
    Assert.Null(parsed.CustomSeconds);
    Assert.Null(parsed.ExpiresOn);
    Assert.Contains(parsed.Errors, error => error.Contains("複数"));
  }

  [Fact]
  public void 期限の重複は期限なしで誤りになる()
  {
    var parsed = DisplayDurationParser.Parse(@"D:\Signage\案内_20260101_20260202.pdf");
    Assert.Null(parsed.ExpiresOn);
    Assert.Null(parsed.CustomSeconds);
    Assert.Contains(parsed.Errors, error => error.Contains("複数"));
  }

  [Fact]
  public void カメラ番号の末尾数字は誤りにしない()
  {
    var parsed = DisplayDurationParser.Parse(@"D:\Signage\IMG_2716.jpg");
    Assert.Null(parsed.CustomSeconds);
    Assert.Null(parsed.ExpiresOn);
    Assert.Empty(parsed.Errors);
  }

  [Fact]
  public void 桁不足の日付は誤りになる()
  {
    var parsed = DisplayDurationParser.Parse(@"D:\Signage\案内_2026923.pdf");
    Assert.Null(parsed.ExpiresOn);
    Assert.NotEmpty(parsed.Errors);
  }

  [Fact]
  public void 途中の連番は期限と秒数の解釈に影響しない()
  {
    var parsed = DisplayDurationParser.Parse(@"D:\Signage\001_会社方針_20_20260904.pdf");
    Assert.Equal(20, parsed.CustomSeconds);
    Assert.Equal(new DateOnly(2026, 9, 4), parsed.ExpiresOn);
    Assert.Empty(parsed.Errors);
  }

  [Fact]
  public void 期限切れファイルはプレイリスト対象外()
  {
    var path = @"D:\Signage\案内_20260904.pdf";
    Assert.True(DisplayDurationParser.IsPlayable(path, new DateOnly(2026, 9, 4)));
    Assert.False(DisplayDurationParser.IsPlayable(path, new DateOnly(2026, 9, 5)));
    Assert.True(DisplayDurationParser.IsPlayable(@"D:\Signage\案内.pdf", new DateOnly(2026, 9, 5)));
  }

  [Theory]
  [InlineData(2026, 9, 4, 2026, 9, 4, false)]
  [InlineData(2026, 9, 4, 2026, 9, 5, true)]
  [InlineData(2026, 9, 4, 2026, 9, 3, false)]
  public void 期限切れ判定は当日まで再生し翌日以降を切る(
    int expireYear,
    int expireMonth,
    int expireDay,
    int todayYear,
    int todayMonth,
    int todayDay,
    bool expectedExpired)
  {
    var expiresOn = new DateOnly(expireYear, expireMonth, expireDay);
    var today = new DateOnly(todayYear, todayMonth, todayDay);
    Assert.Equal(expectedExpired, DisplayDurationParser.IsExpired(expiresOn, today));
  }

  [Fact]
  public void FormatIssuesMessageは誤りが無ければnull()
  {
    Assert.Null(DisplayDurationParser.FormatIssuesMessage(
    [
      @"D:\Signage\案内.pdf",
      @"D:\Signage\案内_20_20260904.pdf",
      @"D:\Signage\IMG_2716.jpg"
    ]));
  }

  [Fact]
  public void FormatIssuesMessageは案内と対象ファイル名を出す()
  {
    var message = DisplayDurationParser.FormatIssuesMessage(
    [
      @"D:\Signage\案内_20261301.pdf",
      @"D:\Signage\写真_20.jpg"
    ]);

    Assert.NotNull(message);
    Assert.Contains("このファイルの表示期限の指定方法が誤っていますのでファイル名を修正してください。", message);
    Assert.Contains("○○_20261231", message);
    Assert.Contains("○○_20_20260212", message);
    Assert.Contains("案内_20261301.pdf", message);
    Assert.DoesNotContain("写真_20.jpg", message);
    Assert.Contains(
      "案内_2026923.pdf",
      DisplayDurationParser.FormatIssuesMessage([@"D:\Signage\案内_2026923.pdf"]));
  }
}
