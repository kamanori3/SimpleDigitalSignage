using System.IO;
using KS_Signage.Licensing;
using KS_Signage.Services;

namespace KS_Signage.Tests;

public class LicenseServiceTests
{
  [Fact]
  public void 埋め込み公開鍵は検証器として読める()
  {
    var verifier = new AccessKeyVerifier(AccessKeyPublicKey.Pem);
    Assert.False(verifier.Verify("not-a-key").IsValid);
  }

  [Fact]
  public void 壊れたキーは無効()
  {
    var logDir = Path.Combine(Path.GetTempPath(), "KS_SignageTests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(logDir);
    try
    {
      var logger = new FileLogger(logDir);
      var result = LicenseService.VerifyAccessKey("PDS1.abc.def", logger);
      Assert.False(result.IsValid);
      Assert.Null(result.Payload);
      Assert.Equal(
        "アクセスキーが正しくありません。メールで届いた文字列をそのまま貼り付けてください。",
        LicenseService.FormatInvalidKeyMessage());
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

  [Fact]
  public void 別鍵で署名したキーは製品公開鍵では無効()
  {
    var (privatePem, _) = AccessKeyKeyPair.CreatePems();
    var signer = new AccessKeySigner(privatePem);
    var key = signer.Sign(new AccessKeyPayload(
      AccessKeyPayload.CurrentVersion,
      "C-0001",
      "例団体",
      new DateOnly(2027, 9, 12),
      LicensePlan.Standard));

    var logDir = Path.Combine(Path.GetTempPath(), "KS_SignageTests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(logDir);
    try
    {
      var logger = new FileLogger(logDir);
      Assert.False(LicenseService.VerifyAccessKey(key, logger).IsValid);
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

  [Fact]
  public void リポジトリの秘密鍵があれば製品公開鍵で検証できる()
  {
    var privatePath = FindRepoPrivateKey();
    if (privatePath is null)
    {
      return;
    }

    var signer = new AccessKeySigner(File.ReadAllText(privatePath));
    var key = signer.Sign(new AccessKeyPayload(
      AccessKeyPayload.CurrentVersion,
      "C-SMOKE",
      "検証",
      new DateOnly(2027, 9, 12),
      LicensePlan.Standard));

    var logDir = Path.Combine(Path.GetTempPath(), "KS_SignageTests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(logDir);
    try
    {
      var logger = new FileLogger(logDir);
      var result = LicenseService.VerifyAccessKey(key, logger);
      Assert.True(result.IsValid);
      Assert.Equal("C-SMOKE", result.Payload!.SerialNumber);
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

  private static string? FindRepoPrivateKey()
  {
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir is not null)
    {
      var candidate = Path.Combine(dir.FullName, "secrets", "access-key-private.pem");
      if (File.Exists(candidate))
      {
        return candidate;
      }

      dir = dir.Parent;
    }

    return null;
  }
}
