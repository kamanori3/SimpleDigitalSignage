namespace PdfSignage.Licensing;

/// <summary>
/// レジストリと LocalAppData から読んだ試用開始日を一本化する。
/// 片方を消しても、残っているより早い日付を採用する。
/// </summary>
public static class TrialMarkerResolver
{
  /// <summary>
  /// 採用する初回起動日と、各ストアへ書き戻すかを返す。
  /// </summary>
  public static DateOnly Resolve(
    DateOnly? registryDate,
    DateOnly? fileDate,
    DateOnly today,
    out bool writeRegistry,
    out bool writeFile)
  {
    if (registryDate is null && fileDate is null)
    {
      writeRegistry = true;
      writeFile = true;
      return today;
    }

    DateOnly chosen;
    if (registryDate is null)
    {
      chosen = fileDate!.Value;
    }
    else if (fileDate is null)
    {
      chosen = registryDate.Value;
    }
    else
    {
      chosen = registryDate.Value <= fileDate.Value ? registryDate.Value : fileDate.Value;
    }

    writeRegistry = registryDate != chosen;
    writeFile = fileDate != chosen;
    return chosen;
  }
}
