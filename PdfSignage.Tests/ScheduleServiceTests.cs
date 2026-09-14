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
}
