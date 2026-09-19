using KS_Signage.Services;

namespace KS_Signage.Tests;

public class ScheduleTimeHelperTests
{
  [Theory]
  [InlineData("18:00", 18, 0)]
  [InlineData("09:30", 9, 30)]
  [InlineData("0:05", 0, 5)]
  [InlineData(" 18:00 ", 18, 0)]
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
