namespace PdfSignage.Licensing;

/// <summary>
/// 複数ストアから読んだ試用開始日を一本化する。
/// 欠けている／遅い側へ、残っているより早い日付を書き戻す。
/// </summary>
public static class TrialMarkerResolver
{
  /// <summary>
  /// 採用する初回起動日と、各ストアへ書き戻すかを返す。
  /// <paramref name="writeFlags"/> の長さと並びは <paramref name="storedDates"/> と同じ。
  /// </summary>
  public static DateOnly Resolve(
    IReadOnlyList<DateOnly?> storedDates,
    DateOnly today,
    out bool[] writeFlags)
  {
    ArgumentNullException.ThrowIfNull(storedDates);
    if (storedDates.Count == 0)
    {
      throw new ArgumentException("ストアが 1 件以上必要です。", nameof(storedDates));
    }

    DateOnly? chosen = null;
    foreach (var date in storedDates)
    {
      if (date is { } value && (chosen is null || value < chosen.Value))
      {
        chosen = value;
      }
    }

    if (chosen is null)
    {
      writeFlags = CreateFilledFlags(storedDates.Count, fill: true);
      return today;
    }

    var adopted = chosen.Value;
    writeFlags = new bool[storedDates.Count];
    for (var i = 0; i < storedDates.Count; i++)
    {
      writeFlags[i] = storedDates[i] != adopted;
    }

    return adopted;
  }

  private static bool[] CreateFilledFlags(int count, bool fill)
  {
    var flags = new bool[count];
    Array.Fill(flags, fill);
    return flags;
  }
}
