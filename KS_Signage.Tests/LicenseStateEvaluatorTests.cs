using KS_Signage.Licensing;

namespace KS_Signage.Tests;

public class LicenseStateEvaluatorTests
{
  private static readonly DateOnly FirstLaunch = new(2026, 9, 12);
  private static readonly DateOnly TrialEnd = new(2026, 11, 10);
  private static readonly AccessKeySigner Signer;
  private static readonly AccessKeyVerifier Verifier;

  static LicenseStateEvaluatorTests()
  {
    var (privatePem, publicPem) = AccessKeyKeyPair.CreatePems();
    Signer = new AccessKeySigner(privatePem);
    Verifier = new AccessKeyVerifier(publicPem);
  }

  [Fact]
  public void 初回起動日は試用1日目で残り60日()
  {
    var evaluation = EvaluateKey(null, FirstLaunch);
    Assert.Equal(LicenseStatus.Trial, evaluation.Status);
    Assert.Equal(TrialEnd, evaluation.ExpiresOn);
    Assert.Equal(60, evaluation.RemainingDays);
    Assert.False(evaluation.ShowRenewalBanner);
    Assert.Null(evaluation.Payload);
  }

  [Fact]
  public void 試用最終日は当日まで有効()
  {
    var evaluation = EvaluateKey(null, TrialEnd);
    Assert.Equal(LicenseStatus.Trial, evaluation.Status);
    Assert.Equal(1, evaluation.RemainingDays);
    Assert.False(evaluation.ShowRenewalBanner);
  }

  [Fact]
  public void 試用最終日の翌日は試用切れ()
  {
    var evaluation = EvaluateKey(null, new DateOnly(2026, 11, 11));
    Assert.Equal(LicenseStatus.TrialExpired, evaluation.Status);
    Assert.Equal(TrialEnd, evaluation.ExpiresOn);
    Assert.Equal(0, evaluation.RemainingDays);
    Assert.True(evaluation.ShowRenewalBanner);
  }

  [Fact]
  public void 有効なキーがあれば試用中でも契約中()
  {
    var key = Sign(new DateOnly(2027, 9, 12));
    var evaluation = EvaluateKey(key, FirstLaunch);
    Assert.Equal(LicenseStatus.Licensed, evaluation.Status);
    Assert.Equal(new DateOnly(2027, 9, 12), evaluation.ExpiresOn);
    Assert.Equal(366, evaluation.RemainingDays);
    Assert.False(evaluation.ShowRenewalBanner);
    Assert.NotNull(evaluation.Payload);
  }

  [Fact]
  public void キー期限当日は契約中()
  {
    var expires = new DateOnly(2027, 9, 12);
    var evaluation = EvaluateKey(Sign(expires), expires);
    Assert.Equal(LicenseStatus.Licensed, evaluation.Status);
    Assert.Equal(1, evaluation.RemainingDays);
    Assert.False(evaluation.ShowRenewalBanner);
  }

  [Fact]
  public void キー期限の翌日は契約切れ()
  {
    var expires = new DateOnly(2027, 9, 12);
    var evaluation = EvaluateKey(Sign(expires), new DateOnly(2027, 9, 13));
    Assert.Equal(LicenseStatus.LicenseExpired, evaluation.Status);
    Assert.Equal(expires, evaluation.ExpiresOn);
    Assert.Equal(0, evaluation.RemainingDays);
    Assert.True(evaluation.ShowRenewalBanner);
    Assert.NotNull(evaluation.Payload);
  }

  [Fact]
  public void 不正なキーは試用判定に落ちる()
  {
    var evaluation = EvaluateKey("PDS1.tampered.signature", new DateOnly(2026, 11, 11));
    Assert.Equal(LicenseStatus.TrialExpired, evaluation.Status);
    Assert.Null(evaluation.Payload);
    Assert.True(evaluation.ShowRenewalBanner);
  }

  private static string Sign(DateOnly expiresOn)
  {
    return Signer.Sign(new AccessKeyPayload(
      AccessKeyPayload.CurrentVersion,
      "C-0001",
      "例団体",
      expiresOn,
      LicensePlan.Standard));
  }

  private static LicenseEvaluation EvaluateKey(string? accessKey, DateOnly today)
  {
    return LicenseStateEvaluator.Evaluate(Verifier.Verify(accessKey), FirstLaunch, today);
  }
}
