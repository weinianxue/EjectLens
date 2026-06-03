namespace EjectLens.Models;

/// <summary>
/// Result of a safe-eject attempt for a removable drive.
/// </summary>
public sealed class EjectResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string DriveLetter { get; init; } = string.Empty;
    public DateTime AttemptTime { get; init; } = DateTime.Now;

    /// <summary>
    /// Veto type from CM_Request_Device_Eject, if the request was denied.
    /// </summary>
    public string? VetoType { get; init; }

    /// <summary>
    /// Veto name (friendly description), if provided by Windows.
    /// </summary>
    public string? VetoName { get; init; }

    /// <summary>
    /// Native return code from CM_Request_Device_Eject.
    /// </summary>
    public int NativeReturnCode { get; init; }

    /// <summary>
    /// PNPDeviceID that was passed to the eject API.
    /// </summary>
    public string? PnpDeviceId { get; init; }

    public string ToReportText()
    {
        var lines = new List<string>();
        lines.Add($"  Drive:       {DriveLetter}");
        lines.Add($"  Time:        {AttemptTime:yyyy-MM-dd HH:mm:ss}");
        lines.Add($"  Result:      {(Success ? "Success" : "Failed")}");
        lines.Add($"  Return code: {NativeReturnCode}");

        if (!string.IsNullOrEmpty(PnpDeviceId))
            lines.Add($"  Device:      {PnpDeviceId}");

        if (!Success)
        {
            if (!string.IsNullOrEmpty(VetoType))
                lines.Add($"  Veto type:   {VetoType}");
            if (!string.IsNullOrEmpty(VetoName))
                lines.Add($"  Veto name:   {VetoName}");
        }

        lines.Add($"  Message:     {Message}");
        return string.Join(Environment.NewLine, lines);
    }

    public static EjectResult Failed(string driveLetter, string message, int code = -1)
        => new()
        {
            Success = false,
            DriveLetter = driveLetter,
            Message = message,
            NativeReturnCode = code
        };

    public static EjectResult Ok(string driveLetter, string pnpDeviceId)
        => new()
        {
            Success = true,
            DriveLetter = driveLetter,
            PnpDeviceId = pnpDeviceId,
            Message = "Windows reported that the device can be safely removed.",
            NativeReturnCode = 0
        };
}
