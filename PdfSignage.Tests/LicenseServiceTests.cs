using System.IO;
using PdfSignage.Licensing;
using PdfSignage.Services;

namespace PdfSignage.Tests;

public class LicenseServiceTests
{
  [Fact]
  public void 公開鍵が未設定ならキーは無効()
  {
    Assert.Equal("", AccessKeyPublicKey.Pem);

    var logDir = Path.Combine(Path.GetTempPath(), "PdfSignageTests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(logDir);
    try
    {
      var logger = new FileLogger(logDir);
      var result = LicenseService.VerifyAccessKey("PDS1.abc.def", logger);
      Assert.False(result.IsValid);
      Assert.Null(result.Payload);
      Assert.Equal("この版ではアクセスキーをまだ検証できません。", LicenseService.FormatInvalidKeyMessage());
    }
    finally
    {
      try
      {
        Directory.Delete(logDir, recursive: true);
      }
      catch
      {
        // 一時フォルダの掃除に失敗してもテスト自体は落とさない
      }
    }
  }
}
