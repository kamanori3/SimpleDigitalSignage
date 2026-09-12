namespace PdfSignage.Models;

/// <summary>
/// ファイル名末尾の秒数・再生期限の解析結果。
/// </summary>
public sealed class FilenameSuffixParseResult
{
  public FilenameSuffixParseResult(
    int? customSeconds,
    DateOnly? expiresOn,
    IReadOnlyList<string> errors)
  {
    CustomSeconds = customSeconds;
    ExpiresOn = expiresOn;
    Errors = errors;
  }

  /// <summary>有効なカスタム秒数。無い・誤りのときは null（デフォルト秒数を使う）。</summary>
  public int? CustomSeconds { get; }

  /// <summary>有効な再生期限。無い・誤りのときは null（期限なし）。</summary>
  public DateOnly? ExpiresOn { get; }

  /// <summary>入力誤りの説明（ユーザー向け）。空なら誤りなし。</summary>
  public IReadOnlyList<string> Errors { get; }
}
