using System.Text.Json;
using System.Text.Json.Serialization;
using KS_Signage.Models;

namespace KS_Signage.Services;

public class SettingsService
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  private readonly string _settingsFilePath;

  public SettingsService()
  {
    _settingsFilePath = Path.Combine(AppContext.BaseDirectory, "settings.json");
  }

  public string SettingsFilePath => _settingsFilePath;

  public AppSettings Load()
  {
    if (!File.Exists(_settingsFilePath))
    {
      var defaults = CreateDefaults();
      Save(defaults);
      return defaults;
    }

    try
    {
      var json = File.ReadAllText(_settingsFilePath);
      var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
      if (settings is null)
      {
        return CreateDefaults();
      }

      Normalize(settings);
      return settings;
    }
    catch (Exception ex)
    {
      WriteLoadError(ex);
      return CreateDefaults();
    }
  }

  public void Save(AppSettings settings)
  {
    Normalize(settings);
    var json = JsonSerializer.Serialize(settings, JsonOptions);
    File.WriteAllText(_settingsFilePath, json);
  }

  private static AppSettings CreateDefaults()
  {
    return new AppSettings();
  }

  private static void WriteLoadError(Exception ex)
  {
    try
    {
      var errorPath = Path.Combine(AppContext.BaseDirectory, "settings_load_error.log");
      var message = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 設定読込失敗: {ex.Message}{Environment.NewLine}";
      File.AppendAllText(errorPath, message);
    }
    catch
    {
      // ログ出力に失敗しても起動は継続する
    }
  }

  private static void Normalize(AppSettings settings)
  {
    settings.DefaultDisplaySeconds = Math.Clamp(
      settings.DefaultDisplaySeconds,
      AppSettings.MinDisplaySeconds,
      AppSettings.MaxDisplaySeconds);

    if (string.IsNullOrWhiteSpace(settings.RecoveryMessage))
    {
      settings.RecoveryMessage = "表示を復旧しています";
    }

    settings.AccessKey = string.IsNullOrWhiteSpace(settings.AccessKey)
      ? ""
      : settings.AccessKey.Trim();

    SettingsMigration.UnifyLegacyExitTimes(settings);
  }
}
