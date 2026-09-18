using System.Security;
using Microsoft.Win32;
using KS_Signage.Licensing;

namespace KS_Signage.Services;

/// <summary>
/// 試用開始日をマシン領域とユーザー領域へ二重書きする（ADR 0013 / 0015）。
/// </summary>
public static class TrialMarkerStore
{
  public const string RegistryKeyPath = @"Software\KS_Signage";
  public const string RegistryValueName = "TrialStart";
  public const string FileName = "trial.dat";

  private const int MachineRegistryIndex = 0;
  private const int UserRegistryIndex = 1;
  private const int MachineFileIndex = 2;
  private const int UserFileIndex = 3;

  public static string UserMarkerFilePath =>
    Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "KS_Signage",
      FileName);

  public static string MachineMarkerFilePath =>
    Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
      "KS_Signage",
      FileName);

  public static DateOnly GetOrCreate(FileLogger logger, DateOnly? today = null)
  {
    var todayLocal = today ?? DateOnly.FromDateTime(DateTime.Now);

    try
    {
      DateOnly?[] stored =
      [
        TryReadRegistry(Registry.LocalMachine, "HKLM", logger),
        TryReadRegistry(Registry.CurrentUser, "HKCU", logger),
        TryReadFile(MachineMarkerFilePath, "ProgramData", logger),
        TryReadFile(UserMarkerFilePath, "LocalAppData", logger),
      ];

      var chosen = TrialMarkerResolver.Resolve(stored, todayLocal, out var writeFlags);

      if (writeFlags[MachineRegistryIndex])
      {
        TryWriteRegistry(Registry.LocalMachine, chosen, "HKLM", logger);
      }

      if (writeFlags[UserRegistryIndex])
      {
        TryWriteRegistry(Registry.CurrentUser, chosen, "HKCU", logger);
      }

      if (writeFlags[MachineFileIndex])
      {
        TryWriteFile(MachineMarkerFilePath, chosen, "ProgramData", isMachineScope: true, logger);
      }

      if (writeFlags[UserFileIndex])
      {
        TryWriteFile(UserMarkerFilePath, chosen, "LocalAppData", isMachineScope: false, logger);
      }

      return chosen;
    }
    catch (Exception ex)
    {
      logger.Error("試用開始日の取得に失敗したため、今日を仮の開始日にします。", ex);
      return todayLocal;
    }
  }

  private static DateOnly? TryReadRegistry(RegistryKey hive, string label, FileLogger logger)
  {
    try
    {
      using var key = hive.OpenSubKey(RegistryKeyPath);
      var raw = key?.GetValue(RegistryValueName) as string;
      if (TrialStartCodec.TryDecode(raw, out var date))
      {
        return date;
      }
    }
    catch (Exception ex) when (IsAccessDenied(ex))
    {
      // 標準ユーザーの HKLM は読めない環境もある。想定内。
    }
    catch (Exception ex)
    {
      logger.Error($"試用開始日のレジストリ読込に失敗しました（{label}）。", ex);
    }

    return null;
  }

  private static DateOnly? TryReadFile(string path, string label, FileLogger logger)
  {
    try
    {
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
    catch (Exception ex) when (IsAccessDenied(ex))
    {
      // ProgramData を他ユーザーが作った場合など、読めないことはある。
    }
    catch (Exception ex)
    {
      logger.Error($"試用開始日のファイル読込に失敗しました（{label}）。", ex);
    }

    return null;
  }

  private static void TryWriteRegistry(RegistryKey hive, DateOnly date, string label, FileLogger logger)
  {
    try
    {
      using var key = hive.CreateSubKey(RegistryKeyPath);
      if (key is null)
      {
        if (!IsMachineHive(hive))
        {
          logger.Error($"試用開始日: レジストリキーを作成できませんでした（{label}）。");
        }

        return;
      }

      key.SetValue(RegistryValueName, TrialStartCodec.Encode(date), RegistryValueKind.String);
    }
    catch (Exception ex) when (IsAccessDenied(ex) && IsMachineHive(hive))
    {
      // 管理者に昇格していないと HKLM へは書けない。UAC は出さず、ユーザー領域へ任せる。
    }
    catch (Exception ex)
    {
      logger.Error($"試用開始日のレジストリ書き込みに失敗しました（{label}）。", ex);
    }
  }

  private static void TryWriteFile(
    string path,
    DateOnly date,
    string label,
    bool isMachineScope,
    FileLogger logger)
  {
    try
    {
      var directory = Path.GetDirectoryName(path);
      if (!string.IsNullOrEmpty(directory))
      {
        Directory.CreateDirectory(directory);
      }

      File.WriteAllText(path, TrialStartCodec.Encode(date));
    }
    catch (Exception ex) when (IsAccessDenied(ex) && isMachineScope)
    {
      // ロックダウンされた PC では ProgramData へ書けない。ユーザー領域へ任せる。
    }
    catch (Exception ex)
    {
      logger.Error($"試用開始日のファイル書き込みに失敗しました（{label}）。", ex);
    }
  }

  private static bool IsMachineHive(RegistryKey hive) =>
    ReferenceEquals(hive, Registry.LocalMachine);

  private static bool IsAccessDenied(Exception ex) =>
    ex is UnauthorizedAccessException or SecurityException;
}
