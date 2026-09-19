using KS_Signage.Licensing;

namespace KS_Signage.ViewModels;

/// <summary>
/// 管理画面のライセンス欄に出す文言。問い合わせ時に読み上げられる短い表示だけを持つ。
/// </summary>
public static class LicenseAdminDisplay
{
  public const string MissingValue = "—";

  public static string StatusLabel(LicenseStatus status)
  {
    return status switch
    {
      LicenseStatus.Trial => "試用中",
      LicenseStatus.TrialExpired => "試用期間終了",
      LicenseStatus.Licensed => "契約中",
      LicenseStatus.LicenseExpired => "契約期限切れ",
      _ => status.ToString()
    };
  }

  public static string ExpiresLabel(LicenseEvaluation license)
  {
    ArgumentNullException.ThrowIfNull(license);
    var remaining = license.RemainingDays > 0
      ? $"残り {license.RemainingDays} 日"
      : "残り 0 日";
    return $"{license.ExpiresOn:yyyy-MM-dd}（{remaining}）";
  }

  public static string SerialNumber(AccessKeyPayload? payload)
  {
    return string.IsNullOrEmpty(payload?.SerialNumber)
      ? MissingValue
      : payload.SerialNumber;
  }

  public static string Organization(AccessKeyPayload? payload)
  {
    return string.IsNullOrEmpty(payload?.Organization)
      ? MissingValue
      : payload.Organization;
  }
}
