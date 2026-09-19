namespace KS_Signage.Licensing;

/// <summary>
/// 試用開始日とアクセスキーから決めた、いまの課金状態。
/// </summary>
public sealed class LicenseEvaluation
{
  public LicenseEvaluation(
    LicenseStatus status,
    DateOnly expiresOn,
    int remainingDays,
    AccessKeyPayload? payload)
  {
    Status = status;
    ExpiresOn = expiresOn;
    RemainingDays = remainingDays;
    Payload = payload;
  }

  public LicenseStatus Status { get; }

  /// <summary>試用の最終日、またはキーに書かれた契約期限。</summary>
  public DateOnly ExpiresOn { get; }

  /// <summary>今日を含む残り日数。期限切れなら 0。</summary>
  public int RemainingDays { get; }

  /// <summary>署名が正しいキーがあるときだけ入る（期限切れキーも含む）。</summary>
  public AccessKeyPayload? Payload { get; }

  public bool ShowRenewalBanner =>
    Status is LicenseStatus.TrialExpired or LicenseStatus.LicenseExpired;
}
