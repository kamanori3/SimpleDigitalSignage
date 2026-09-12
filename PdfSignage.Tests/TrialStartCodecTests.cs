using PdfSignage.Licensing;

namespace PdfSignage.Tests;

public class TrialStartCodecTests
{
  [Fact]
  public void 同じ日付は往復する()
  {
    var date = new DateOnly(2026, 9, 12);
    var encoded = TrialStartCodec.Encode(date);
    Assert.True(TrialStartCodec.TryDecode(encoded, out var decoded));
    Assert.Equal(date, decoded);
  }

  [Fact]
  public void 平文の年月日は含まれない()
  {
    var encoded = TrialStartCodec.Encode(new DateOnly(2026, 9, 12));
    Assert.DoesNotContain("20260912", encoded, StringComparison.Ordinal);
    Assert.DoesNotContain("2026-09-12", encoded, StringComparison.Ordinal);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("not-base64")]
  [InlineData("YQ==")]
  public void 壊れた値はデコードできない(string? value)
  {
    Assert.False(TrialStartCodec.TryDecode(value, out _));
  }
}
