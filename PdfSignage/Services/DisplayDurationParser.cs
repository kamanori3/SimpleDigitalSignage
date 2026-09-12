using System.Globalization;
using System.Text.RegularExpressions;
using PdfSignage.Models;

namespace PdfSignage.Services;

/// <summary>
/// ファイル名末尾の <c>_秒数</c> / <c>_YYYYMMDD</c> を解決する。
/// <para>
/// 末尾から最大 2 個の <c>_数字</c> を対象とし、順序は問わない。
/// 1〜3 桁かつ 5〜300 は表示秒数、8 桁かつ実在日は再生期限。
/// 入力誤りは再生を止めず、誤った指定だけ捨てて <see cref="FilenameSuffixParseResult.Errors"/> に残す。
/// <c>IMG_2716.jpg</c> のような 4〜5 桁・9 桁以上はメタデータではないので無視する。
/// 6〜7 桁は 8 桁日付の桁落ちとして誤りにする。
/// </para>
/// </summary>
public static partial class DisplayDurationParser
{
  private const string DateFormat = "yyyyMMdd";

  [GeneratedRegex(@"_(\d+)$", RegexOptions.CultureInvariant)]
  private static partial Regex TrailingDigitsPattern();

  /// <summary>
  /// ファイル名から表示秒数を取得する。該当なしの場合はデフォルト値を返す。
  /// </summary>
  public static int Resolve(string filePath, int defaultSeconds)
  {
    return Parse(filePath).CustomSeconds ?? defaultSeconds;
  }

  /// <summary>
  /// ファイル名に有効な秒数指定（5〜300）があるかどうか。
  /// </summary>
  public static bool HasCustomDuration(string filePath)
  {
    return Parse(filePath).CustomSeconds is not null;
  }

  /// <summary>
  /// 期限日当日は再生し、翌日以降を期限切れとする。
  /// </summary>
  public static bool IsExpired(DateOnly expiresOn, DateOnly today)
  {
    return today > expiresOn;
  }

  /// <summary>
  /// 期限切れでなければプレイリスト対象（入力誤りは対象のまま）。
  /// </summary>
  public static bool IsPlayable(string filePath, DateOnly today)
  {
    var parsed = Parse(filePath);
    return parsed.ExpiresOn is not { } expiresOn || !IsExpired(expiresOn, today);
  }

  /// <summary>
  /// ファイル名末尾の秒数・再生期限・入力誤りを解析する。
  /// </summary>
  public static FilenameSuffixParseResult Parse(string filePath)
  {
    var nameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
    var tokens = CollectTrailingMetadataTokens(nameWithoutExtension);
    return ClassifyTokens(tokens);
  }

  /// <summary>
  /// 複数ファイルの入力誤りを、修正案内と対象ファイル名の本文にまとめる。誤りが無ければ null。
  /// </summary>
  public static string? FormatIssuesMessage(IEnumerable<string> filePaths)
  {
    var blocks = new List<string>();

    foreach (var filePath in filePaths)
    {
      var parsed = Parse(filePath);
      if (parsed.Errors.Count == 0)
      {
        continue;
      }

      blocks.Add(Path.GetFileName(filePath));
    }

    if (blocks.Count == 0)
    {
      return null;
    }

    return "このファイルの表示期限の指定方法が誤っていますのでファイル名を修正してください。"
           + Environment.NewLine
           + "例）○○_20261231、○○_20_20260212"
           + Environment.NewLine
           + Environment.NewLine
           + string.Join(Environment.NewLine, blocks);
  }

  private static List<string> CollectTrailingMetadataTokens(string nameWithoutExtension)
  {
    var tokens = new List<string>();
    var remaining = nameWithoutExtension;

    while (tokens.Count < 2)
    {
      var match = TrailingDigitsPattern().Match(remaining);
      if (!match.Success)
      {
        break;
      }

      var digits = match.Groups[1].Value;
      if (!IsMetadataCandidate(digits))
      {
        break;
      }

      tokens.Insert(0, digits);
      remaining = remaining[..^match.Length];
    }

    return tokens;
  }

  /// <summary>
  /// 秒数候補（1〜3 桁）、日付候補（8 桁）、桁落ちした日付（6〜7 桁）をメタデータとして扱う。
  /// </summary>
  private static bool IsMetadataCandidate(string digits)
  {
    return digits.Length is >= 1 and <= 3 or >= 6 and <= 8;
  }

  private static FilenameSuffixParseResult ClassifyTokens(IReadOnlyList<string> tokens)
  {
    var errors = new List<string>();
    var durationCount = 0;
    var dateCount = 0;
    int? validSeconds = null;
    DateOnly? validDate = null;

    foreach (var token in tokens)
    {
      if (token.Length is 6 or 7)
      {
        errors.Add($"日付 {token} は8桁（YYYYMMDD）ではありません。期限指定を無視します。");
        continue;
      }

      if (token.Length == 8)
      {
        dateCount++;
        if (TryParseExpireDate(token, out var date))
        {
          validDate = date;
        }
        else
        {
          errors.Add($"日付 {token} は存在しません。期限指定を無視します。");
        }

        continue;
      }

      durationCount++;
      if (int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)
          && seconds is >= AppSettings.MinDisplaySeconds and <= AppSettings.MaxDisplaySeconds)
      {
        validSeconds = seconds;
      }
      else
      {
        errors.Add(
          $"表示秒数 {token} は {AppSettings.MinDisplaySeconds}〜{AppSettings.MaxDisplaySeconds} の範囲外です。デフォルト秒数を使います。");
      }
    }

    if (durationCount >= 2)
    {
      errors.Add("表示秒数が複数あります。デフォルト秒数を使います。");
      validSeconds = null;
    }

    if (dateCount >= 2)
    {
      errors.Add("再生期限が複数あります。期限指定を無視します。");
      validDate = null;
    }

    return new FilenameSuffixParseResult(validSeconds, validDate, errors);
  }

  private static bool TryParseExpireDate(string token, out DateOnly date)
  {
    return DateOnly.TryParseExact(
      token,
      DateFormat,
      CultureInfo.InvariantCulture,
      DateTimeStyles.None,
      out date);
  }
}
