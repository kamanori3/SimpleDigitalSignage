using System.Security.Cryptography;

namespace KS_Signage.Licensing;

/// <summary>
/// 公開鍵でアクセスキーを検証する。期限判定はしない。
/// </summary>
public sealed class AccessKeyVerifier
{
  private readonly ECDsa _ecdsa;

  public AccessKeyVerifier(string publicKeyPem)
  {
    if (string.IsNullOrWhiteSpace(publicKeyPem))
    {
      throw new ArgumentException("公開鍵 PEM は必須です。", nameof(publicKeyPem));
    }

    _ecdsa = ECDsa.Create();
    _ecdsa.ImportFromPem(publicKeyPem);
  }

  public AccessKeyVerifyResult Verify(string? accessKey)
  {
    if (!AccessKeyCodec.TrySplit(accessKey, out var payloadUtf8, out var signatureDer))
    {
      return AccessKeyVerifyResult.Invalid;
    }

    try
    {
      if (!_ecdsa.VerifyData(
            payloadUtf8,
            signatureDer,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence))
      {
        return AccessKeyVerifyResult.Invalid;
      }
    }
    catch (CryptographicException)
    {
      return AccessKeyVerifyResult.Invalid;
    }

    if (!AccessKeyCodec.TryParsePayload(payloadUtf8, out var payload) || payload is null)
    {
      return AccessKeyVerifyResult.Invalid;
    }

    return new AccessKeyVerifyResult(true, payload);
  }
}
