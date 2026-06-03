namespace EjectLens.Utils;

/// <summary>
/// Redacts personally identifiable paths in reports for privacy.
/// Replaces the current user's home directory with %USERPROFILE%.
/// </summary>
public static class PathRedactor
{
    private static readonly string UserProfilePath =
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>
    /// Replaces occurrences of the user's profile directory with %USERPROFILE%.
    /// Case-insensitive replacement for Windows paths.
    /// </summary>
    public static string Redact(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        if (string.IsNullOrEmpty(UserProfilePath))
            return text;

        // Use case-insensitive replacement since Windows paths are case-insensitive.
        return ReplaceCaseInsensitive(text, UserProfilePath, "%USERPROFILE%");
    }

    /// <summary>
    /// Redacts the user profile path and other known sensitive paths.
    /// More specific paths (AppData, LocalAppData) are redacted first
    /// to avoid partial matches from the broader UserProfile replacement.
    /// </summary>
    public static string RedactFull(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = text;

        // Redact more specific paths first, then broader ones.
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrEmpty(localAppData))
            result = ReplaceCaseInsensitive(result, localAppData, "%LOCALAPPDATA%");

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (!string.IsNullOrEmpty(appData))
            result = ReplaceCaseInsensitive(result, appData, "%APPDATA%");

        result = Redact(result);

        return result;
    }

    private static string ReplaceCaseInsensitive(string text, string oldValue, string newValue)
    {
        var idx = 0;
        var result = new System.Text.StringBuilder();
        var textSpan = text.AsSpan();
        var oldSpan = oldValue.AsSpan();

        while (idx < text.Length)
        {
            if (idx + oldValue.Length <= text.Length &&
                textSpan.Slice(idx, oldValue.Length).Equals(oldSpan, StringComparison.OrdinalIgnoreCase))
            {
                result.Append(newValue);
                idx += oldValue.Length;
            }
            else
            {
                result.Append(text[idx]);
                idx++;
            }
        }

        return result.ToString();
    }
}
