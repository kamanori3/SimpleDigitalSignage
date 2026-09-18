namespace KS_Signage.Licensing;

internal static class Base64Url
{
  public static string Encode(byte[] data)
  {
    return Convert.ToBase64String(data)
      .TrimEnd('=')
      .Replace('+', '-')
      .Replace('/', '_');
  }

  public static bool TryDecode(string text, out byte[] data)
  {
    data = [];
    if (string.IsNullOrEmpty(text))
    {
      return false;
    }

    var padded = text.Replace('-', '+').Replace('_', '/');
    switch (padded.Length % 4)
    {
      case 0:
        break;
      case 2:
        padded += "==";
        break;
      case 3:
        padded += "=";
        break;
      default:
        return false;
    }

    try
    {
      data = Convert.FromBase64String(padded);
      return true;
    }
    catch (FormatException)
    {
      return false;
    }
  }
}
