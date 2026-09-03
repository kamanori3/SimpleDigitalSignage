using PdfSignage.Services;

namespace PdfSignage.Tests;

public class ScheduleTimeHelperTests
{
  [Theory]
  [InlineData("18:00", 3, "18:03")]
  [InlineData("09:30", 30, "10:00")]
  [InlineData("23:59", 2, "00:01")]   // 日付跨ぎ
  [InlineData("00:01", -2, "23:59")]  // 負の加算も日付跨ぎ
  public void 分を加算してHH_mm形式で返す(string time, int minutes, string expected)
  {
    Assert.Equal(expected, ScheduleTimeHelper.AddMinutes(time, minutes));
  }

  [Fact]
  public void 解析できない文字列はそのまま返す()
  {
    Assert.Equal("あいうえお", ScheduleTimeHelper.AddMinutes("あいうえお", 3));
  }

  [Fact]
  public void PC電源オフの既定値はアプリ終了の3分後()
  {
    Assert.Equal(3, ScheduleTimeHelper.DefaultPcShutdownDelayMinutes);
    Assert.Equal("18:03", ScheduleTimeHelper.GetDefaultPcShutdownTime("18:00"));
  }

  [Theory]
  [InlineData("18:00", 18, 0)]
  [InlineData("09:30", 9, 30)]
  [InlineData("0:05", 0, 5)]
  [InlineData(" 18:00 ", 18, 0)]  // 前後の空白は許容
  public void 有効な時刻をTimeSpanに変換する(string time, int hours, int minutes)
  {
    Assert.True(ScheduleTimeHelper.TryParseScheduleTime(time, out var parsed));
    Assert.Equal(new TimeSpan(hours, minutes, 0), parsed);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("18時")]
  [InlineData("25:00")]
  public void 無効な時刻はfalseを返す(string? time)
  {
    Assert.False(ScheduleTimeHelper.TryParseScheduleTime(time, out _));
  }
}
