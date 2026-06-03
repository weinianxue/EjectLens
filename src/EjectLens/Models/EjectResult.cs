namespace EjectLens.Models;

/// <summary>
/// Result of a safe-eject attempt for a removable drive.
/// Contains detailed information about every candidate device node tried.
/// </summary>
public sealed class EjectResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string DriveLetter { get; init; } = string.Empty;
    public DateTime AttemptTime { get; init; } = DateTime.Now;

    public int ReturnCode { get; init; }
    public string ReturnCodeName => Native.DeviceEjectNative.GetReturnCodeName(ReturnCode);

    public string? VetoType { get; init; }
    public string? VetoName { get; init; }

    public string? SelectedDeviceInstanceId { get; init; }

    /// <summary>
    /// The candidate that succeeded, if any.
    /// </summary>
    public EjectCandidate? SuccessfulCandidate { get; init; }

    /// <summary>
    /// All candidates that were attempted, in order.
    /// </summary>
    public List<EjectCandidate> AttemptedCandidates { get; init; } = [];

    /// <summary>
    /// Human-readable suggestions for next steps based on the failure mode.
    /// </summary>
    public List<string> SuggestedNextSteps { get; init; } = [];

    /// <summary>
    /// Whether the safe volume-level fallback was attempted.
    /// </summary>
    public bool FallbackAttempted { get; init; }
    public string? FallbackResult { get; init; }

    public string ToReportText()
    {
        var lines = new List<string>();
        lines.Add($"  Drive:       {DriveLetter}");
        lines.Add($"  Time:        {AttemptTime:yyyy-MM-dd HH:mm:ss}");
        lines.Add($"  Result:      {(Success ? "Success" : "Failed")}");
        lines.Add($"  Return code: {ReturnCode} / {ReturnCodeName}");

        if (SuccessfulCandidate != null)
            lines.Add($"  Successful:  {SuccessfulCandidate.ToSummary()}");

        if (!Success)
        {
            if (!string.IsNullOrEmpty(VetoType))
                lines.Add($"  Veto type:   {VetoType}");
            if (!string.IsNullOrEmpty(VetoName))
                lines.Add($"  Veto name:   {VetoName}");
        }

        lines.Add($"  Message:     {Message}");

        if (AttemptedCandidates.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("  Attempted device nodes:");
            foreach (var c in AttemptedCandidates)
                lines.Add($"    {c.ToSummary()}");
        }

        if (FallbackAttempted)
        {
            lines.Add(string.Empty);
            lines.Add($"  Volume fallback: {(FallbackResult ?? "not attempted")}");
        }

        if (SuggestedNextSteps.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("  Suggested next steps:");
            foreach (var step in SuggestedNextSteps)
                lines.Add($"    - {step}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    public static EjectResult Failed(string driveLetter, string message, int code = -1)
        => new()
        {
            Success = false,
            DriveLetter = driveLetter,
            Message = message,
            ReturnCode = code,
            SuggestedNextSteps =
            [
                "Try running EjectLens as administrator.",
                "Close any open files and folders on the drive.",
                "Use 'Scan Selected Path' to check for file locks.",
                "Wait a few seconds and try again.",
            ]
        };

    public static EjectResult Ok(string driveLetter, EjectCandidate candidate)
        => new()
        {
            Success = true,
            DriveLetter = driveLetter,
            Message = "Windows reported that the device can be safely removed.",
            ReturnCode = 0,
            SuccessfulCandidate = candidate,
            SelectedDeviceInstanceId = candidate.DeviceInstanceId
        };
}
