using Microsoft.Win32;
using PdfSignage.Licensing;

namespace PdfSignage.Services;

/// <summary>
/// 試用開始日を HKCU と LocalAppData に二重書きする（ADR 0013）。
/// </summary>
public static class TrialMarkerStore
{
  public const string RegistryKeyPath = @"Software\PdfSignage";
  public const string RegistryValueName = "TrialStart";
  public const string FileName = "trial.dat";

  public static string MarkerFilePath =>
    Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "PdfSignage",
      FileName);

  public static DateOnly GetOrCreate(FileLogger logger, DateOnly? today = null)
  {
    var todayLocal = today ?? DateOnly.FromDateTime(DateTime.Now);

    try
    {
      var registryDate = TryReadRegistry(logger);
      var fileDate = TryReadFile(logger);
      var chosen = TrialMarkerResolver.Resolve(
        registryDate,
        fileDate,
        todayLocal,
        out var writeRegistry,
        out var writeFile);

      if (writeRegistry)
      {
        TryWriteRegistry(chosen, logger);
      }

      if (writeFile)
      {
        TryWriteFile(chosen, logger);
      }

      return chosen;
    }
    catch (Exception ex)
    {
      logger.Error("試用開始日の取得に失敗したため、今日を仮の開始日にします。", ex);
      return todayLocal;
    }
  }

  private static DateOnly? TryReadRegistry(FileLogger logger)
  {
    try
    {
      using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
      var raw = key?.GetValue(RegistryValueName) as string;
      if (TrialStartCodec.TryDecode(raw, out var date))
      {
        return date;
      }
    }
    catch (Exception ex)
    {
      logger.Error("試用開始日のレジストリ読込に失敗しました。", ex);
    }

    return null;
  }

  private static DateOnly? TryReadFile(FileLogger logger)
  {
    try
    {
      var path = MarkerFilePath;
      if (!File.Exists(path))
      {
        return null;
      }

      var raw = File.ReadAllText(path);
      if (TrialStartCodec.TryDecode(raw, out var date))
      {
        return date;
      }
    }
    catch (Exception ex)
    {
      logger.Error("試用開始日のファイル読込に失敗しました。", ex);
    }

    return null;
  }

  private static void TryWriteRegistry(DateOnly date, FileLogger logger)
  {
    try
    {
      using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
      if (key is null)
      {
        logger.Error("試用開始日: レジストリキーを作成できませんでした。");
        return;
      }

      key.SetValue(RegistryValueName, TrialStartCodec.Encode(date), RegistryValueKind.String);
    }
    catch (Exception ex)
    {
      logger.Error("試用開始日のレジストリ書き込みに失敗しました。", ex);
    }
  }

  private static void TryWriteFile(DateOnly date, FileLogger logger)
  {
    try
    {
      var path = MarkerFilePath;
      var directory = Path.GetDirectoryName(path);
      if (!string.IsNullOrEmpty(directory))
      {
        Directory.CreateDirectory(directory);
      }

      File.WriteAllText(path, TrialStartCodec.Encode(date));
    }
    catch (Exception ex)
    {
      logger.Error("試用開始日のファイル書き込みに失敗しました。", ex);
    }
  }
}
