using System.ComponentModel;
using System.Diagnostics;

namespace EjectLens.Services;

/// <summary>
/// Provides information about running processes.
/// Handles permission errors gracefully — on Windows, accessing
/// certain process properties requires elevation.
/// </summary>
public static class ProcessInfoService
{
    /// <summary>
    /// Common Windows processes that frequently hold file handles
    /// during eject operations. We flag these with gentle guidance.
    /// </summary>
    private static readonly HashSet<string> WellKnownBlockerProcesses = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "System",
        "svchost.exe",
        "explorer.exe",
        "SearchIndexer.exe",
        "SearchProtocolHost.exe",
        "SearchFilterHost.exe",
        "MsMpEng.exe",
        "OneDrive.exe",
        "FileCoAuth.exe",
        "LockApp.exe",
        "dllhost.exe"
    };

    /// <summary>
    /// Check whether a process with the given PID is currently running.
    /// </summary>
    public static bool IsProcessRunning(int pid)
    {
        if (pid <= 0) return false;

        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (ArgumentException) { return false; }
        catch (InvalidOperationException) { return false; }
    }

    /// <summary>
    /// Get the executable path for a running process.
    /// Returns null if the process has exited or cannot be accessed.
    /// </summary>
    public static string? GetProcessPath(int pid)
    {
        if (pid <= 0) return null;

        try
        {
            using var process = Process.GetProcessById(pid);
            if (process.HasExited) return null;

            try { return process.MainModule?.FileName; }
            catch (Win32Exception) { return null; }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get the process name for a PID if running.
    /// Returns null if the process has exited.
    /// </summary>
    public static string? GetProcessName(int pid)
    {
        if (pid <= 0) return null;

        try
        {
            using var process = Process.GetProcessById(pid);
            if (process.HasExited) return null;
            return process.ProcessName + ".exe";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returns a friendly advisory message for well-known system processes
    /// that commonly appear as eject blockers.
    /// </summary>
    public static string? GetAdvisoryMessage(string processName)
    {
        if (string.IsNullOrEmpty(processName))
            return null;

        var name = Path.GetFileName(processName);

        if (name.Equals("System", StringComparison.OrdinalIgnoreCase))
            return "System process — a handle may be held by a driver or service. "
                 + "Check for recently accessed files on the drive.";

        if (name.Equals("svchost.exe", StringComparison.OrdinalIgnoreCase))
            return "Windows Service Host — a background service may be accessing the drive. "
                 + "This usually resolves itself within a few minutes.";

        if (name.Equals("explorer.exe", StringComparison.OrdinalIgnoreCase))
            return "File Explorer — close any Explorer windows showing the drive contents "
                 + "and try again.";

        if (name.Equals("SearchIndexer.exe", StringComparison.OrdinalIgnoreCase)
            || name.Equals("SearchProtocolHost.exe", StringComparison.OrdinalIgnoreCase)
            || name.Equals("SearchFilterHost.exe", StringComparison.OrdinalIgnoreCase))
            return "Windows Search is indexing the drive. You can pause indexing "
                 + "from Control Panel → Indexing Options.";

        if (name.Equals("MsMpEng.exe", StringComparison.OrdinalIgnoreCase))
            return "Windows Defender is scanning the drive. This is temporary.";

        if (name.Equals("OneDrive.exe", StringComparison.OrdinalIgnoreCase)
            || name.Equals("FileCoAuth.exe", StringComparison.OrdinalIgnoreCase))
            return "OneDrive or a cloud sync client may be syncing files on the drive. "
                 + "Pause sync and try again.";

        if (name.Equals("dllhost.exe", StringComparison.OrdinalIgnoreCase))
            return "COM Surrogate — a thumbnail provider or shell extension may be "
                 + "accessing files on the drive.";

        return null;
    }

    public static bool IsWellKnownBlocker(string processName)
        => WellKnownBlockerProcesses.Contains(Path.GetFileName(processName));
}
