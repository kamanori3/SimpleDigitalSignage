using System.IO;
using PdfSignage.Models;
using PdfSignage.Services;

namespace PdfSignage.Tests;

public class SettingsValidatorTests
{
  private static bool Validate(
    out string error,
    string watchFolder = @"D:\Signage",
    bool allowNetwork = false,
    int seconds = 15,
    bool appExitEnabled = false,
    string appExitTime = "18:00",
    bool shutdownEnabled = false,
    string shutdownTime = "18:03",
    string recoveryMessage = "表示を復旧しています",
    Func<string, DriveType>? getDriveType = null)
  {
    getDriveType ??= _ => DriveType.Fixed;
    return SettingsValidator.TryValidate(
      watchFolder,
      allowNetwork,
      seconds,
      appExitEnabled, appExitTime,
      shutdownEnabled, shutdownTime,
      recoveryMessage,
      out error,
      getDriveType);
  }

  [Fact]
  public void 既定値の組み合わせは有効()
  {
    Assert.True(Validate(out var error));
    Assert.Equal("", error);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public void 監視フォルダが空なら無効(string path)
  {
    Assert.False(Validate(out var error, watchFolder: path));
    Assert.Contains("監視フォルダ", error);
  }

  [Theory]
  [InlineData(AppSettings.MinDisplaySeconds - 1)]
  [InlineData(AppSettings.MaxDisplaySeconds + 1)]
  [InlineData(0)]
  public void 表示秒数が範囲外なら無効(int seconds)
  {
    Assert.False(Validate(out var error, seconds: seconds));
    Assert.Contains("デフォルト表示秒数", error);
  }

  [Theory]
  [InlineData(AppSettings.MinDisplaySeconds)]
  [InlineData(AppSettings.MaxDisplaySeconds)]
  public void 表示秒数の境界値は有効(int seconds)
  {
    Assert.True(Validate(out _, seconds: seconds));
  }

  [Fact]
  public void 無効な終了時刻はチェックされない()
  {
    // スケジュールを無効にしている場合、時刻欄の内容は問わない
    Assert.True(Validate(out _, appExitEnabled: false, appExitTime: "でたらめ"));
  }

  [Fact]
  public void 有効化した終了時刻の書式は検証される()
  {
    Assert.False(Validate(out var error, appExitEnabled: true, appExitTime: "18時"));
    Assert.Contains("アプリ終了時刻", error);
  }

  [Fact]
  public void 有効化した電源オフ時刻の書式は検証される()
  {
    Assert.False(Validate(out var error, shutdownEnabled: true, shutdownTime: "24:00"));
    Assert.Contains("PC 電源オフ時刻", error);
  }

  [Fact]
  public void 復帰不能時メッセージは必須()
  {
    Assert.False(Validate(out var error, recoveryMessage: "  "));
    Assert.Contains("復帰不能時メッセージ", error);
  }

  [Fact]
  public void ネットワーク不許可ならUNCの監視フォルダは無効()
  {
    Assert.False(Validate(out var error, watchFolder: @"\\server\share\signage"));
    Assert.Contains("この PC 内", error);
  }

  [Fact]
  public void ネットワーク不許可なら割り当てドライブは無効()
  {
    Assert.False(Validate(
      out var error,
      watchFolder: @"Z:\Signage",
      getDriveType: _ => DriveType.Network));
    Assert.Contains("この PC 内", error);
  }

  [Fact]
  public void ネットワーク許可ならUNCの監視フォルダは有効()
  {
    Assert.True(Validate(out _, watchFolder: @"\\server\share\signage", allowNetwork: true));
  }

  [Theory]
  [InlineData("00:00", true)]
  [InlineData("23:59", true)]
  [InlineData("9:05", true)]
  [InlineData("24:00", false)]
  [InlineData("18:60", false)]
  [InlineData("1800", false)]
  public void IsValidTimeはHH_mm形式のみ受け付ける(string value, bool expected)
  {
    Assert.Equal(expected, SettingsValidator.IsValidTime(value));
  }

  [Fact]
  public void NormalizeTimeは前後の空白を落とす()
  {
    Assert.Equal("18:00", SettingsValidator.NormalizeTime("  18:00 "));
  }
}
