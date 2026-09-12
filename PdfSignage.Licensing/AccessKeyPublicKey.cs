namespace PdfSignage.Licensing;

/// <summary>
/// 製品のアクセスキー検証用公開鍵（ECDSA P-256 PEM）。
/// 秘密鍵はリポジトリに置かない。発行ツールで生成した公開鍵をここに埋め込む。
/// 空のあいだはキーをすべて無効とし、試用判定に落とす。
/// </summary>
public static class AccessKeyPublicKey
{
  public const string Pem = "";
}
