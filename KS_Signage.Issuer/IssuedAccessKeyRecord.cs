using System.Globalization;
using System.Text.Json.Serialization;
using KS_Signage.Licensing;

namespace KS_Signage.Issuer;

/// <summary>
/// 発行したアクセスキー 1 件。メールに貼った文字列そのものを残す（再署名すると別文字列になる）。
/// </summary>
public sealed class IssuedAccessKeyRecord
{
  public DateTimeOffset IssuedAt { get; init; }

  public string SerialNumber { get; init; } = "";

  public string? Organization { get; init; }

  public string Plan { get; init; } = "";

  public string ExpiresOn { get; init; } = "";

  public string AccessKey { get; init; } = "";

  [JsonIgnore]
  public string PlanLabel =>
    string.Equals(Plan, "site", StringComparison.OrdinalIgnoreCase) ? "サイト" : "標準";

  [JsonIgnore]
  public string OrganizationLabel =>
    string.IsNullOrWhiteSpace(Organization) ? "—" : Organization.Trim();

  [JsonIgnore]
  public string IssuedAtLabel =>
    IssuedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

  public static IssuedAccessKeyRecord From(
    AccessKeyPayload payload,
    string accessKey,
    DateTimeOffset issuedAt)
  {
    ArgumentNullException.ThrowIfNull(payload);
    ArgumentException.ThrowIfNullOrWhiteSpace(accessKey);

    return new IssuedAccessKeyRecord
    {
      IssuedAt = issuedAt,
      SerialNumber = payload.SerialNumber,
      Organization = payload.Organization,
      Plan = payload.Plan == LicensePlan.Site ? "site" : "standard",
      ExpiresOn = payload.ExpiresOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
      AccessKey = accessKey.Trim()
    };
  }
}
