namespace PdfSignage.Licensing;

/// <summary>
/// アクセスキーに載せる契約内容。署名の対象。
/// </summary>
public sealed class AccessKeyPayload
{
  public const int CurrentVersion = 1;

  public AccessKeyPayload(
    int version,
    string serialNumber,
    string? organization,
    DateOnly expiresOn,
    LicensePlan plan)
  {
    if (version != CurrentVersion)
    {
      throw new ArgumentOutOfRangeException(nameof(version), "形式バージョンは 1 のみです。");
    }

    if (string.IsNullOrWhiteSpace(serialNumber))
    {
      throw new ArgumentException("契約の通し番号は必須です。", nameof(serialNumber));
    }

    Version = version;
    SerialNumber = serialNumber.Trim();
    Organization = string.IsNullOrWhiteSpace(organization) ? null : organization.Trim();
    ExpiresOn = expiresOn;
    Plan = plan;
  }

  public int Version { get; }

  public string SerialNumber { get; }

  public string? Organization { get; }

  /// <summary>契約期限。その日の終わりまで有効。</summary>
  public DateOnly ExpiresOn { get; }

  public LicensePlan Plan { get; }
}
