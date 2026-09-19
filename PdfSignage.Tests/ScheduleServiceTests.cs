using PdfSignage.Services;

namespace PdfSignage.Tests;

public class ScheduleServiceTests
{
  [Fact]
  public void 同じ日は日付変更ではない()
  {
    var date = new DateOnly(2026, 9, 12);
    Assert.False(ScheduleService.HasDateChanged(date, date));
  }

  [Fact]
  public void 翌日とスリープ越しの日付変更を検知する()
  {
    Assert.True(ScheduleService.HasDateChanged(new DateOnly(2026, 9, 12), new DateOnly(2026, 9, 13)));
    Assert.True(ScheduleService.HasDateChanged(new DateOnly(2026, 9, 12), new DateOnly(2026, 9, 14)));
  }

  [Theory]
  [InlineData("17:59:00", "18:00:00", "18:00:00", true)]
  [InlineData("17:59:30", "18:00:30", "18:00:00", true)]
  [InlineData("18:00:00", "18:00:30", "18:00:00", false)]
  [InlineData("17:00:00", "17:30:00", "18:00:00", false)]
  [InlineData("18:00:00", "18:00:00", "18:00:00", false)]
  [InlineData("00:00:00", "00:00:30", "00:00:00", false)]
  [InlineData("23:59:00", "00:01:00", "00:00:00", true)]
  [InlineData("23:50:00", "00:01:00", "23:55:00", true)]
  [InlineData("23:59:00", "00:01:00", "23:58:00", false)]
  [InlineData("23:59:00", "00:01:00", "00:02:00", false)]
  public void 設定時刻を跨いだときだけtrue(
    string previous,
    string current,
    string scheduled,
    bool expected)
  {
    Assert.Equal(
      expected,
      ScheduleService.CrossedScheduleTime(
        TimeSpan.Parse(previous),
        TimeSpan.Parse(current),
        TimeSpan.Parse(scheduled)));
  }

  [Fact]
  public void 終了時刻が未設定なら発火しない()
  {
    var previous = TimeSpan.Parse("17:59:00");
    var current = TimeSpan.Parse("18:00:00");
    Assert.False(ScheduleService.ShouldRequestExit(null, previous, current));
    Assert.False(ScheduleService.ShouldRequestExit("", previous, current));
    Assert.False(ScheduleService.ShouldRequestExit("  ", previous, current));
  }

  [Fact]
  public void 無効な終了時刻なら発火しない()
  {
    Assert.False(ScheduleService.ShouldRequestExit(
      "18時",
      TimeSpan.Parse("17:59:00"),
      TimeSpan.Parse("18:00:00")));
  }

  [Fact]
  public void 有効な終了時刻を跨げば発火する()
  {
    Assert.True(ScheduleService.ShouldRequestExit(
      "18:00",
      TimeSpan.Parse("17:59:00"),
      TimeSpan.Parse("18:00:30")));
  }

  [Fact]
  public void 終了時刻の前後空白は許容する()
  {
    Assert.True(ScheduleService.ShouldRequestExit(
      " 18:00 ",
      TimeSpan.Parse("17:59:00"),
      TimeSpan.Parse("18:00:00")));
  }
}
