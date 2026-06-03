namespace EjectLens.Models;

/// <summary>
/// Full diagnostic report for clipboard export or file save.
/// </summary>
public sealed class ReportModel
{
    public string AppVersion { get; init; } = string.Empty;
    public string WindowsVersion { get; init; } = string.Empty;
    public string SelectedDrive { get; init; } = string.Empty;
    public DateTime ScanTime { get; init; }
    public string TimeRangeDescription { get; init; } = string.Empty;

    public List<EjectBlockEvent> MatchedEvents { get; init; } = [];
    public List<EjectBlockEvent> PossiblyRelatedEvents { get; init; } = [];
    public List<EjectBlockEvent> UnmatchedEvents { get; init; } = [];
    public List<LockingProcessInfo> FileLockResults { get; init; } = [];
    public string? FileLockScanPath { get; init; }
    public string? FileLockError { get; init; }
}
