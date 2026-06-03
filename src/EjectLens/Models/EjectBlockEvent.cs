namespace EjectLens.Models;

/// <summary>
/// Parsed representation of a Microsoft-Windows-Kernel-PnP Event ID 225,
/// which indicates a process blocked device ejection.
/// </summary>
public sealed class EjectBlockEvent
{
    public DateTime EventTime { get; init; }
    public int ProcessId { get; init; }
    public string ApplicationPath { get; init; } = string.Empty;
    public string CommandLine { get; init; } = string.Empty;
    public string DeviceInstanceId { get; init; } = string.Empty;
    public string RawMessageSummary { get; init; } = string.Empty;

    /// <summary>
    /// Whether the XML was successfully parsed to extract structured fields.
    /// </summary>
    public bool IsParsed { get; init; }

    /// <summary>
    /// Match status relative to the selected drive.
    /// </summary>
    public MatchStatus MatchStatus { get; set; } = MatchStatus.Unmatched;

    /// <summary>
    /// Whether the process is still running.
    /// </summary>
    public bool IsProcessRunning { get; set; }

    public string ProcessName
    {
        get
        {
            if (!string.IsNullOrEmpty(ApplicationPath))
                return Path.GetFileName(ApplicationPath);

            if (!string.IsNullOrEmpty(CommandLine))
            {
                // Command line may start with a quoted path (for paths with spaces)
                // or an unquoted path. Extract the executable path by finding .exe.
                var cmd = CommandLine.Trim();
                string exePath;

                if (cmd.StartsWith('"'))
                {
                    var endQuote = cmd.IndexOf('"', 1);
                    exePath = endQuote > 0 ? cmd[1..endQuote] : cmd[1..];
                }
                else
                {
                    // Find the first .exe and take everything up to that point.
                    var exeIdx = cmd.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                    if (exeIdx > 0)
                        exePath = cmd[..(exeIdx + 4)];
                    else
                        exePath = cmd;
                }

                var name = Path.GetFileName(exePath);
                if (!string.IsNullOrEmpty(name))
                    return name;
            }

            return "(unknown)";
        }
    }
}

public enum MatchStatus
{
    Matched,
    PossiblyRelated,
    Unmatched
}
