using System.Text;
using KS_Signage.Licensing;

namespace KS_Signage.Tests;

public class AccessKeyCodecTests
{
  private static readonly AccessKeySigner Signer;
  private static readonly AccessKeyVerifier Verifier;

  static AccessKeyCodecTests()
  {
    var (privatePem, publicPem) = AccessKeyKeyPair.CreatePems();
    Signer = new AccessKeySigner(privatePem);
    Verifier = new AccessKeyVerifier(publicPem);
  }

  [Fact]
  public void 署名したキーは検証に通りペイロードが戻る()
  {
    var payload = new AccessKeyPayload(
      AccessKeyPayload.CurrentVersion,
      "C-0001",
      "例団体",
      new DateOnly(2027, 9, 12),
      LicensePlan.Standard);

    var key = Signer.Sign(payload);
    Assert.StartsWith(AccessKeyCodec.Prefix, key, StringComparison.Ordinal);

    var result = Verifier.Verify(key);
    Assert.True(result.IsValid);
    Assert.NotNull(result.Payload);
    Assert.Equal("C-0001", result.Payload.SerialNumber);
    Assert.Equal("例団体", result.Payload.Organization);
    Assert.Equal(new DateOnly(2027, 9, 12), result.Payload.ExpiresOn);
    Assert.Equal(LicensePlan.Standard, result.Payload.Plan);
  }

  [Fact]
  public void 前後の空白は貼り付け用に無視する()
  {
    var payload = Sample();
    var key = "  " + Signer.Sign(payload) + "\r\n";
    Assert.True(Verifier.Verify(key).IsValid);
  }

  [Fact]
  public void サイトプランも往復する()
  {
    var payload = new AccessKeyPayload(
      AccessKeyPayload.CurrentVersion,
      "C-0002",
      null,
      new DateOnly(2027, 1, 1),
      LicensePlan.Site);

    var result = Verifier.Verify(Signer.Sign(payload));
    Assert.True(result.IsValid);
    Assert.Null(result.Payload!.Organization);
    Assert.Equal(LicensePlan.Site, result.Payload.Plan);
  }

  [Fact]
  public void 署名を改ざんすると無効()
  {
    var key = Signer.Sign(Sample());
    var lastDot = key.LastIndexOf('.');
    var signature = key[(lastDot + 1)..];
    var flipped = (signature[0] == 'A' ? 'B' : 'A') + signature[1..];
    var tampered = key[..(lastDot + 1)] + flipped;
    Assert.False(Verifier.Verify(tampered).IsValid);
  }

  [Fact]
  public void 期限を書き換えると署名が合わず無効()
  {
    var key = Signer.Sign(Sample());
    Assert.True(AccessKeyCodec.TrySplit(key, out var payloadUtf8, out var signature));

    var json = Encoding.UTF8.GetString(payloadUtf8);
    var tamperedJson = json.Replace("2027-09-12", "2099-01-01", StringComparison.Ordinal);
    var tamperedKey = AccessKeyCodec.Combine(Encoding.UTF8.GetBytes(tamperedJson), signature);

    Assert.False(Verifier.Verify(tamperedKey).IsValid);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("not-a-key")]
  [InlineData("PDS1.")]
  [InlineData("PDS1.abc")]
  public void 形式が壊れていれば無効(string? key)
  {
    Assert.False(Verifier.Verify(key).IsValid);
  }

  [Fact]
  public void 別の鍵で署名したものは無効()
  {
    var other = new AccessKeySigner(AccessKeyKeyPair.CreatePems().PrivatePem);
    var key = other.Sign(Sample());
    Assert.False(Verifier.Verify(key).IsValid);
  }

  private static AccessKeyPayload Sample()
  {
    return new AccessKeyPayload(
      AccessKeyPayload.CurrentVersion,
      "C-0001",
      "例団体",
      new DateOnly(2027, 9, 12),
      LicensePlan.Standard);
  }
}
