using System.IO;
using PdfSignage.Services;

namespace PdfSignage.Tests;

public class WatchFolderLocationPolicyTests
{
  [Fact]
  public void ネットワーク許可ならUNCも通る()
  {
    Assert.True(WatchFolderLocationPolicy.IsAllowedOnThisPc(
      @"\\server\share\signage", allowNetworkWatchFolder: true));
  }

  [Theory]
  [InlineData(@"\\server\share\signage")]
  [InlineData(@"\\server\share")]
  [InlineData(@"//server/share/signage")]
  [InlineData(@"\\?\UNC\server\share\signage")]
  public void ネットワーク不許可ならUNCは拒否する(string path)
  {
    Assert.False(WatchFolderLocationPolicy.IsAllowedOnThisPc(path, allowNetworkWatchFolder: false));
  }

  [Fact]
  public void ネットワーク不許可なら割り当てドライブは拒否する()
  {
    Assert.False(WatchFolderLocationPolicy.IsAllowedOnThisPc(
      @"Z:\Signage",
      allowNetworkWatchFolder: false,
      getDriveType: _ => DriveType.Network));
  }

  [Theory]
  [InlineData(DriveType.Fixed)]
  [InlineData(DriveType.Removable)]
  public void 本体ディスクとUSBは許可する(DriveType driveType)
  {
    Assert.True(WatchFolderLocationPolicy.IsAllowedOnThisPc(
      @"D:\Signage",
      allowNetworkWatchFolder: false,
      getDriveType: _ => driveType));
  }

  [Fact]
  public void 存在しないドライブはネットワークではないので許可する()
  {
    Assert.True(WatchFolderLocationPolicy.IsAllowedOnThisPc(
      @"D:\Signage",
      allowNetworkWatchFolder: false,
      getDriveType: _ => DriveType.NoRootDirectory));
  }

  [Fact]
  public void 空文字は拒否する()
  {
    Assert.False(WatchFolderLocationPolicy.IsAllowedOnThisPc("  ", allowNetworkWatchFolder: false));
  }

  [Fact]
  public void 拡張プレフィックスのローカルパスはUNCではない()
  {
    Assert.False(WatchFolderLocationPolicy.IsUncPath(@"\\?\D:\Signage"));
  }
}
