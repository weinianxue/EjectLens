namespace EjectLens.Models;

/// <summary>
/// Represents a removable drive detected in the system.
/// </summary>
public sealed class DriveInfoModel
{
    public string DriveLetter { get; init; } = string.Empty;
    public string VolumeLabel { get; init; } = string.Empty;
    public string FileSystem { get; init; } = string.Empty;
    public long TotalBytes { get; init; }
    public long AvailableBytes { get; init; }
    public string? VolumeGuidPath { get; init; }

    public string TotalFormatted => FormatBytes(TotalBytes);
    public string AvailableFormatted => FormatBytes(AvailableBytes);

    public string DisplayText =>
        string.IsNullOrEmpty(VolumeLabel)
            ? $"{DriveLetter} ({FileSystem}, {TotalFormatted})"
            : $"{DriveLetter} - {VolumeLabel} ({FileSystem}, {TotalFormatted})";

    private static string FormatBytes(long bytes)
    {
        if (bytes < 0) return "N/A";

        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < suffixes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {suffixes[order]}";
    }
}
