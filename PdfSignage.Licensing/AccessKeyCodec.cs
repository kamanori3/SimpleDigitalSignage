using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PdfSignage.Licensing;

/// <summary>
/// アクセスキー文字列 <c>PDS1.{payload}.{signature}</c> の組み立てと分解。
/// </summary>
public static class AccessKeyCodec
{
  public const string Prefix = "PDS1.";

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  public static byte[] SerializePayload(AccessKeyPayload payload)
  {
    var dto = new PayloadDto
    {
      Version = payload.Version,
      SerialNumber = payload.SerialNumber,
      Organization = payload.Organization,
      ExpiresOn = payload.ExpiresOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
      Plan = ToPlanString(payload.Plan)
    };

    return JsonSerializer.SerializeToUtf8Bytes(dto, JsonOptions);
  }

  public static bool TryParsePayload(byte[] jsonUtf8, out AccessKeyPayload? payload)
  {
    payload = null;
    PayloadDto? dto;
    try
    {
      dto = JsonSerializer.Deserialize<PayloadDto>(jsonUtf8, JsonOptions);
    }
    catch (JsonException)
    {
      return false;
    }

    if (dto is null
        || dto.Version != AccessKeyPayload.CurrentVersion
        || string.IsNullOrWhiteSpace(dto.SerialNumber)
        || string.IsNullOrWhiteSpace(dto.ExpiresOn)
        || !DateOnly.TryParseExact(
          dto.ExpiresOn,
          "yyyy-MM-dd",
          CultureInfo.InvariantCulture,
          DateTimeStyles.None,
          out var expiresOn)
        || !TryParsePlan(dto.Plan, out var plan))
    {
      return false;
    }

    payload = new AccessKeyPayload(
      dto.Version,
      dto.SerialNumber,
      dto.Organization,
      expiresOn,
      plan);
    return true;
  }

  public static string Combine(byte[] payloadUtf8, byte[] signatureDer)
  {
    return Prefix + Base64Url.Encode(payloadUtf8) + "." + Base64Url.Encode(signatureDer);
  }

  public static bool TrySplit(string? accessKey, out byte[] payloadUtf8, out byte[] signatureDer)
  {
    payloadUtf8 = [];
    signatureDer = [];
    if (string.IsNullOrWhiteSpace(accessKey))
    {
      return false;
    }

    var trimmed = accessKey.Trim();
    if (!trimmed.StartsWith(Prefix, StringComparison.Ordinal))
    {
      return false;
    }

    var rest = trimmed[Prefix.Length..];
    var separator = rest.LastIndexOf('.');
    if (separator <= 0 || separator == rest.Length - 1)
    {
      return false;
    }

    var payloadPart = rest[..separator];
    var signaturePart = rest[(separator + 1)..];
    if (payloadPart.Contains('.', StringComparison.Ordinal))
    {
      return false;
    }

    return Base64Url.TryDecode(payloadPart, out payloadUtf8)
           && Base64Url.TryDecode(signaturePart, out signatureDer);
  }

  internal static string ToPlanString(LicensePlan plan)
  {
    return plan == LicensePlan.Site ? "site" : "standard";
  }

  internal static bool TryParsePlan(string? value, out LicensePlan plan)
  {
    if (string.Equals(value, "standard", StringComparison.Ordinal))
    {
      plan = LicensePlan.Standard;
      return true;
    }

    if (string.Equals(value, "site", StringComparison.Ordinal))
    {
      plan = LicensePlan.Site;
      return true;
    }

    plan = default;
    return false;
  }

  private sealed class PayloadDto
  {
    [JsonPropertyName("v")]
    public int Version { get; set; }

    [JsonPropertyName("n")]
    public string SerialNumber { get; set; } = "";

    [JsonPropertyName("org")]
    public string? Organization { get; set; }

    [JsonPropertyName("exp")]
    public string ExpiresOn { get; set; } = "";

    [JsonPropertyName("plan")]
    public string Plan { get; set; } = "";
  }
}
