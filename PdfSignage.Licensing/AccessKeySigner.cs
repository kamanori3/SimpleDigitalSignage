using System.Security.Cryptography;

namespace PdfSignage.Licensing;

/// <summary>
/// 秘密鍵でアクセスキーを発行する。アプリ本体には置かない。
/// </summary>
public sealed class AccessKeySigner
{
  private readonly ECDsa _ecdsa;

  public AccessKeySigner(string privateKeyPem)
  {
    if (string.IsNullOrWhiteSpace(privateKeyPem))
    {
      throw new ArgumentException("秘密鍵 PEM は必須です。", nameof(privateKeyPem));
    }

    _ecdsa = ECDsa.Create();
    _ecdsa.ImportFromPem(privateKeyPem);
  }

  public string Sign(AccessKeyPayload payload)
  {
    ArgumentNullException.ThrowIfNull(payload);
    var payloadUtf8 = AccessKeyCodec.SerializePayload(payload);
    var signatureDer = _ecdsa.SignData(
      payloadUtf8,
      HashAlgorithmName.SHA256,
      DSASignatureFormat.Rfc3279DerSequence);
    return AccessKeyCodec.Combine(payloadUtf8, signatureDer);
  }
}
