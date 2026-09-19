using System.Globalization;
using System.Text.RegularExpressions;

namespace KS_Signage.Issuer;

/// <summary>
/// 台帳から次の契約通し番号（<c>C-0001</c> 形式）を決める。
/// </summary>
public static class SerialNumberSuggester
{
  private static readonly Regex Pattern = new(@"^C-(\d+)$", RegexOptions.CultureInvariant);

  public static string SuggestNext(IEnumerable<IssuedAccessKeyRecord> records)
  {
    ArgumentNullException.ThrowIfNull(records);

    var max = 0;
    var found = false;
    foreach (var record in records)
    {
      if (record is null || string.IsNullOrWhiteSpace(record.SerialNumber))
      {
        continue;
      }

      var match = Pattern.Match(record.SerialNumber.Trim());
      if (!match.Success)
      {
        continue;
      }

      if (!int.TryParse(
            match.Groups[1].Value,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var n))
      {
        continue;
      }

      found = true;
      if (n > max)
      {
        max = n;
      }
    }

    var next = found ? max + 1 : 1;
    return string.Create(CultureInfo.InvariantCulture, $"C-{next:D4}");
  }
}
