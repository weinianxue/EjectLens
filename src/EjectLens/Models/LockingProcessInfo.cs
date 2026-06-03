namespace EjectLens.Models;

/// <summary>
/// Information about a process holding a file lock, obtained via Restart Manager.
/// </summary>
public sealed class LockingProcessInfo
{
    public int ProcessId { get; init; }
    public string ProcessName { get; init; } = string.Empty;
    public string? ExecutablePath { get; init; }
    public string? ApplicationType { get; init; }
    public string? ServiceShortName { get; init; }

    public string DisplayName =>
        string.IsNullOrEmpty(ExecutablePath)
            ? $"{ProcessName} (PID {ProcessId})"
            : $"{ProcessName} (PID {ProcessId}) — {ExecutablePath}";
}
