namespace EjectLens.Models;

/// <summary>
/// A device node candidate for safe-eject that was identified and tried.
/// </summary>
public sealed class EjectCandidate
{
    public string DeviceInstanceId { get; init; } = string.Empty;
    public string? FriendlyName { get; init; }

    /// <summary>
    /// Descriptive level: DiskDrive, USBSTORParent, USBParent, Parent1, etc.
    /// </summary>
    public string Level { get; init; } = string.Empty;

    public bool IsLikelyRemovableTarget { get; init; }

    /// <summary>
    /// Why this node was selected as a candidate.
    /// </summary>
    public string WhySelected { get; init; } = string.Empty;

    /// <summary>
    /// Result of CM_Request_Device_EjectW on this node.
    /// </summary>
    public int ReturnCode { get; set; }

    public bool WasTried { get; set; }
    public string? VetoType { get; set; }
    public string? VetoName { get; set; }

    public string ReturnCodeName => Native.DeviceEjectNative.GetReturnCodeName(ReturnCode);

    public string ToSummary() =>
        $"[{Level}] {(WasTried ? $"RC={ReturnCode}({ReturnCodeName})" : "not tried")}" +
        $"{(VetoType != null ? $" veto={VetoType}" : "")}" +
        $" — {DeviceInstanceId}";
}
