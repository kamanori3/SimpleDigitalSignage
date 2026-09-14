namespace PdfSignage.Licensing;

/// <summary>
/// 製品のアクセスキー検証用公開鍵（ECDSA P-256 PEM）。
/// 秘密鍵はリポジトリに置かない。差し替えは Issuer の gen-keys 後にこの定数を更新する。
/// </summary>
public static class AccessKeyPublicKey
{
  public const string Pem = """
    -----BEGIN PUBLIC KEY-----
    MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEVAINCGH0vEOsvwaBHfqoWR0T9W4s
    Q53WGYSxFmUrx5CPtq4MG4nSNBvlJOugmw0VHkLDqittUqs/rysuuyKKwA==
    -----END PUBLIC KEY-----
    """;
}
