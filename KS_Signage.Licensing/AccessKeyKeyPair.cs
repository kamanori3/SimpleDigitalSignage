using System.Security.Cryptography;

namespace KS_Signage.Licensing;

/// <summary>
/// ECDSA P-256 のキーペア生成。発行ツールとテストが使う。
/// </summary>
public static class AccessKeyKeyPair
{
  public static (string PrivatePem, string PublicPem) CreatePems()
  {
    using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    return (ecdsa.ExportPkcs8PrivateKeyPem(), ecdsa.ExportSubjectPublicKeyInfoPem());
  }
}
