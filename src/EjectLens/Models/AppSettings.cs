namespace EjectLens.Models;

/// <summary>
/// Application settings persisted to %APPDATA%\EjectLens\settings.json.
/// All properties have sensible defaults so a missing file does not break the app.
/// </summary>
public sealed class AppSettings
{
    // ── General ──────────────────────────────────────────────────

    /// <summary>
    /// UI language: "system", "en-US", or "zh-CN".
    /// </summary>
    public string Language { get; set; } = "system";

    // ── Window ───────────────────────────────────────────────────

    public bool RememberWindowSize { get; set; } = true;

    /// <summary>
    /// "small" (900x600), "medium" (1100x700), "large" (1300x850).
    /// </summary>
    public string DefaultWindowSize { get; set; } = "medium";

    public bool StartMaximized { get; set; }

    public int LastWindowWidth { get; set; } = 1100;
    public int LastWindowHeight { get; set; } = 700;
    public int LastWindowX { get; set; } = -1;
    public int LastWindowY { get; set; } = -1;

    // ── Appearance ───────────────────────────────────────────────

    /// <summary>
    /// "system", "light", or "dark".
    /// </summary>
    public string Theme { get; set; } = "system";

    // ── Behavior ─────────────────────────────────────────────────

    public bool ConfirmBeforeEject { get; set; } = true;
    public bool RefreshAfterEject { get; set; } = true;
    public bool RememberLastDrive { get; set; }
    public string LastSelectedDrive { get; set; } = string.Empty;

    /// <summary>
    /// "15min", "1hour", "2hours", "24hours".
    /// </summary>
    public string DefaultTimeRange { get; set; } = "2hours";

    // ── Helpers ──────────────────────────────────────────────────

    public Size GetDefaultSize() => DefaultWindowSize switch
    {
        "small" => new Size(900, 600),
        "large" => new Size(1300, 850),
        _ => new Size(1100, 700)
    };

    public static AppSettings CreateDefaults() => new();
}
