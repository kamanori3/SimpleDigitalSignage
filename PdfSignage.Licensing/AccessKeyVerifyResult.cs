namespace PdfSignage.Licensing;

/// <summary>
/// 署名と形式の検証結果。期限切れかどうかは <see cref="LicenseStateEvaluator"/> が判定する。
/// </summary>
public sealed class AccessKeyVerifyResult
{
  public static AccessKeyVerifyResult Invalid { get; } = new(false, null);

  public AccessKeyVerifyResult(bool isValid, AccessKeyPayload? payload)
  {
    if (isValid && payload is null)
    {
      throw new ArgumentException("有効な検証結果にはペイロードが必要です。", nameof(payload));
    }

    IsValid = isValid;
    Payload = payload;
  }

  /// <summary>署名とスキーマが正しい。期限は見ない。</summary>
  public bool IsValid { get; }

  public AccessKeyPayload? Payload { get; }
}
