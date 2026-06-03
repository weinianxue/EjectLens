using System.Text.Json;
using EjectLens.Models;

namespace EjectLens.Services;

/// <summary>
/// Loads and saves AppSettings to a JSON file in %APPDATA%\EjectLens\.
/// Handles corrupt files gracefully by falling back to defaults and
/// keeping a backup of the broken file.
/// </summary>
public class SettingsService
{
    private readonly string _settingsDir;
    private readonly string _settingsPath;
    private readonly string _backupPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Creates a SettingsService using the default %APPDATA%\EjectLens directory.
    /// </summary>
    public SettingsService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EjectLens"))
    {
    }

    /// <summary>
    /// Creates a SettingsService using a custom directory. Useful for testing.
    /// </summary>
    public SettingsService(string settingsDir)
    {
        _settingsDir = settingsDir;
        _settingsPath = Path.Combine(_settingsDir, "settings.json");
        _backupPath = Path.Combine(_settingsDir, "settings.json.bak");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return AppSettings.CreateDefaults();

            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

            return settings ?? AppSettings.CreateDefaults();
        }
        catch
        {
            // Corrupt settings file — back it up and use defaults.
            TryBackupCorruptFile();
            return AppSettings.CreateDefaults();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(_settingsDir);

            var json = JsonSerializer.Serialize(settings, JsonOptions);

            // Atomic write: write to temp file then rename.
            var tempPath = _settingsPath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _settingsPath, overwrite: true);
        }
        catch
        {
            // Settings are not critical — fail silently.
        }
    }

    private void TryBackupCorruptFile()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                Directory.CreateDirectory(_settingsDir);
                File.Move(_settingsPath, _backupPath, overwrite: true);
            }
        }
        catch
        {
            // Could not back up — not critical.
        }
    }
}
