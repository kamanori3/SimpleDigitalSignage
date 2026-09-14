using System.Globalization;
using System.Text;

namespace PdfSignage.Licensing;

/// <summary>
/// 試用開始日の難読化。本格 DRM ではなく、平文をそのまま置かないため。
/// </summary>
public static class TrialStartCodec
{
  private static readonly byte[] Mask = Encoding.UTF8.GetBytes("PdfSignage.Trial");

  public static string Encode(DateOnly date)
  {
    var plain = Encoding.UTF8.GetBytes(date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
    return Convert.ToBase64String(Xor(plain));
  }

  public static bool TryDecode(string? obfuscated, out DateOnly date)
  {
    date = default;
    if (string.IsNullOrWhiteSpace(obfuscated))
    {
      return false;
    }

    try
    {
      var bytes = Convert.FromBase64String(obfuscated.Trim());
      var plain = Encoding.UTF8.GetString(Xor(bytes));
      return DateOnly.TryParseExact(
        plain,
        "yyyyMMdd",
        CultureInfo.InvariantCulture,
        DateTimeStyles.None,
        out date);
    }
    catch (FormatException)
    {
      return false;
    }
  }

  private static byte[] Xor(byte[] data)
  {
    var result = new byte[data.Length];
    for (var i = 0; i < data.Length; i++)
    {
      result[i] = (byte)(data[i] ^ Mask[i % Mask.Length]);
    }

    return result;
  }
}
