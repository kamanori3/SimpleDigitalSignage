namespace KS_Signage.Licensing;

/// <summary>
/// 製品のアクセスキー検証用公開鍵（ECDSA P-256 PEM）。
/// 秘密鍵はリポジトリに置かない。差し替えは Issuer の gen-keys 後にこの定数を更新する。
/// </summary>
public static class AccessKeyPublicKey
{
  public const string Pem = """
    -----BEGIN PUBLIC KEY-----
    MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEkCTdWpqRBTkYwGmTk4eAEdbIJDtN
    mDhHJb73qbc0aznUbU4sC5Xc1d6y+AYygbsFZ4GcV8LGIGfLmc8hW5rnoA==
    -----END PUBLIC KEY-----
    """;
}
