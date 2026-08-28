using System.Globalization;
using System.Text;

namespace PdfSignage.Services;

/// <summary>
/// ファイル名の自然順ソート（001, 002, 010 の順）
/// </summary>
public sealed class NaturalStringComparer : IComparer<string>
{
  public static NaturalStringComparer Instance { get; } = new();

  public int Compare(string? x, string? y)
  {
    if (ReferenceEquals(x, y))
    {
      return 0;
    }

    if (x is null)
    {
      return -1;
    }

    if (y is null)
    {
      return 1;
    }

    var indexX = 0;
    var indexY = 0;

    while (indexX < x.Length && indexY < y.Length)
    {
      var charX = x[indexX];
      var charY = y[indexY];

      if (char.IsDigit(charX) && char.IsDigit(charY))
      {
        var chunkX = ReadNumber(x, ref indexX);
        var chunkY = ReadNumber(y, ref indexY);

        var numberCompare = ulong.Parse(chunkX, CultureInfo.InvariantCulture)
          .CompareTo(ulong.Parse(chunkY, CultureInfo.InvariantCulture));

        if (numberCompare != 0)
        {
          return numberCompare;
        }
      }
      else
      {
        var compare = char.ToUpperInvariant(charX).CompareTo(char.ToUpperInvariant(charY));
        if (compare != 0)
        {
          return compare;
        }

        indexX++;
        indexY++;
      }
    }

    return x.Length.CompareTo(y.Length);
  }

  private static string ReadNumber(string value, ref int index)
  {
    var builder = new StringBuilder();
    while (index < value.Length && char.IsDigit(value[index]))
    {
      builder.Append(value[index]);
      index++;
    }

    return builder.Length > 0 ? builder.ToString() : "0";
  }
}
