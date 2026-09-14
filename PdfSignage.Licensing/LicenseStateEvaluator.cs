namespace PdfSignage.Licensing;

/// <summary>
/// アクセスキー検証結果と試用開始日から、いまの課金状態を決める。
/// </summary>
public static class LicenseStateEvaluator
{
  /// <summary>初回起動日を 1 日目とした試用日数。</summary>
  public const int TrialDays = 60;

  public static DateOnly TrialExpiresOn(DateOnly firstLaunchOn)
  {
    return firstLaunchOn.AddDays(TrialDays - 1);
  }

  public static LicenseEvaluation Evaluate(
    AccessKeyVerifyResult keyResult,
    DateOnly firstLaunchOn,
    DateOnly today)
  {
    ArgumentNullException.ThrowIfNull(keyResult);

    if (keyResult.IsValid && keyResult.Payload is not null)
    {
      var payload = keyResult.Payload;
      var remaining = RemainingInclusive(payload.ExpiresOn, today);
      var status = today <= payload.ExpiresOn
        ? LicenseStatus.Licensed
        : LicenseStatus.LicenseExpired;
      return new LicenseEvaluation(status, payload.ExpiresOn, remaining, payload);
    }

    var trialEnd = TrialExpiresOn(firstLaunchOn);
    var trialRemaining = RemainingInclusive(trialEnd, today);
    var trialStatus = today <= trialEnd
      ? LicenseStatus.Trial
      : LicenseStatus.TrialExpired;
    return new LicenseEvaluation(trialStatus, trialEnd, trialRemaining, payload: null);
  }

  private static int RemainingInclusive(DateOnly expiresOn, DateOnly today)
  {
    var days = expiresOn.DayNumber - today.DayNumber + 1;
    return days < 0 ? 0 : days;
  }
}
