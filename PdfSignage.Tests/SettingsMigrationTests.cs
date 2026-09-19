using System.Text.Json;
using System.Text.Json.Serialization;
using PdfSignage.Models;
using PdfSignage.Services;

namespace PdfSignage.Tests;

public class SettingsMigrationTests
{
  [Fact]
  public void 終了時刻だけあればそのまま残す()
  {
    var settings = new AppSettings
    {
      AppExitTime = "18:00",
      PcShutdownTime = null
    };

    SettingsMigration.UnifyLegacyExitTimes(settings);

    Assert.Equal("18:00", settings.AppExitTime);
    Assert.Null(settings.PcShutdownTime);
  }

  [Fact]
  public void 電源オフ時刻だけあれば終了時刻へ移す()
  {
    var settings = new AppSettings
    {
      AppExitTime = null,
      PcShutdownTime = "22:00"
    };

    SettingsMigration.UnifyLegacyExitTimes(settings);

    Assert.Equal("22:00", settings.AppExitTime);
    Assert.Null(settings.PcShutdownTime);
  }

  [Fact]
  public void 両方あるときは終了時刻を残す()
  {
    var settings = new AppSettings
    {
      AppExitTime = "18:00",
      PcShutdownTime = "18:03"
    };

    SettingsMigration.UnifyLegacyExitTimes(settings);

    Assert.Equal("18:00", settings.AppExitTime);
    Assert.Null(settings.PcShutdownTime);
  }

  [Fact]
  public void どちらも無ければ無効のまま()
  {
    var settings = new AppSettings();

    SettingsMigration.UnifyLegacyExitTimes(settings);

    Assert.Null(settings.AppExitTime);
    Assert.Null(settings.PcShutdownTime);
  }

  [Fact]
  public void 空白の終了時刻は電源オフ時刻で補う()
  {
    var settings = new AppSettings
    {
      AppExitTime = "  ",
      PcShutdownTime = " 19:30 "
    };

    SettingsMigration.UnifyLegacyExitTimes(settings);

    Assert.Equal("19:30", settings.AppExitTime);
    Assert.Null(settings.PcShutdownTime);
  }

  [Fact]
  public void 終了時刻の前後空白は落とす()
  {
    var settings = new AppSettings
    {
      AppExitTime = " 18:00 ",
      PcShutdownTime = "18:03"
    };

    SettingsMigration.UnifyLegacyExitTimes(settings);

    Assert.Equal("18:00", settings.AppExitTime);
    Assert.Null(settings.PcShutdownTime);
  }

  [Fact]
  public void 旧JSONのpcShutdownTimeは読み取れる()
  {
    var json = """{ "appExitTime": null, "pcShutdownTime": "22:00" }""";
    var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
    Assert.NotNull(settings);
    Assert.Equal("22:00", settings.PcShutdownTime);

    SettingsMigration.UnifyLegacyExitTimes(settings);
    Assert.Equal("22:00", settings.AppExitTime);
    Assert.Null(settings.PcShutdownTime);
  }

  [Fact]
  public void 保存時にpcShutdownTimeは書き出さない()
  {
    var settings = new AppSettings
    {
      AppExitTime = "18:00",
      PcShutdownTime = "18:03"
    };

    SettingsMigration.UnifyLegacyExitTimes(settings);
    var json = JsonSerializer.Serialize(settings, JsonOptions);
    Assert.Contains("appExitTime", json);
    Assert.DoesNotContain("pcShutdownTime", json);
  }

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };
}
