using System.Text.RegularExpressions;
using PdfSignage.Models;

namespace PdfSignage.Services;

/// <summary>
/// ファイル名末尾の <c>_秒数</c> から表示時間を解決する。
/// <para>
/// 例: <c>003_イベント案内_120.pdf</c> → 120 秒、<c>ガイダンス_50.jpeg</c> → 50 秒。
/// 拡張子直前の <c>_数字</c> のみを対象とする（5〜300 の範囲内のみ有効）。
/// <c>IMG_2716.jpg</c> のように末尾が意図しない数字の場合はデフォルト秒数を使用する。
/// </para>
/// </summary>
public static partial class DisplayDurationParser
{
  [GeneratedRegex(@"_(\d+)$", RegexOptions.CultureInvariant)]
  private static partial Regex SuffixSecondsPattern();

  /// <summary>
  /// ファイル名から表示秒数を取得する。該当なしの場合はデフォルト値を返す。
  /// </summary>
  /// <param name="filePath">対象ファイルのフルパス</param>
  /// <param name="defaultSeconds">ファイル名に秒数指定がない場合のデフォルト</param>
  /// <returns>表示秒数（カスタム指定が無効な場合はデフォルト値）</returns>
  public static int Resolve(string filePath, int defaultSeconds)
  {
    if (!TryParseCustomSeconds(filePath, out var seconds))
    {
      return defaultSeconds;
    }

    return seconds;
  }

  /// <summary>
  /// ファイル名に有効な秒数指定（5〜300）があるかどうか。
  /// </summary>
  public static bool HasCustomDuration(string filePath)
  {
    return TryParseCustomSeconds(filePath, out _);
  }

  private static bool TryParseCustomSeconds(string filePath, out int seconds)
  {
    seconds = 0;
    var nameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
    var match = SuffixSecondsPattern().Match(nameWithoutExtension);

    if (!match.Success || !int.TryParse(match.Groups[1].Value, out seconds))
    {
      return false;
    }

    return seconds is >= AppSettings.MinDisplaySeconds and <= AppSettings.MaxDisplaySeconds;
  }
}
