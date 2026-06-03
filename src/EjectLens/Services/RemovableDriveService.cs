using System.Runtime.InteropServices;
using EjectLens.Models;
using EjectLens.Native;

namespace EjectLens.Services;

/// <summary>
/// Enumerates removable drives on the system and collects metadata.
/// </summary>
public class RemovableDriveService
{
    /// <summary>
    /// Returns all removable drives currently available.
    /// Gracefully handles drives that may be in an invalid state.
    /// </summary>
    public List<DriveInfoModel> GetRemovableDrives()
    {
        var drives = new List<DriveInfoModel>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Removable && drive.DriveType != DriveType.Unknown)
                continue;

            try
            {
                var model = new DriveInfoModel
                {
                    DriveLetter = drive.Name.TrimEnd('\\'),
                    VolumeLabel = GetSafeVolumeLabel(drive),
                    FileSystem = drive.DriveFormat ?? "Unknown",
                    TotalBytes = GetSafeTotalSize(drive),
                    AvailableBytes = GetSafeAvailableSize(drive),
                    VolumeGuidPath = VolumeMappingService.GetVolumeGuidPath(drive.Name)
                };

                drives.Add(model);
            }
            catch
            {
                // Skip drives we can't read — they might be in a bad state.
                // Still add a minimal entry so the user can see it exists.
                drives.Add(new DriveInfoModel
                {
                    DriveLetter = drive.Name.TrimEnd('\\'),
                    VolumeLabel = "(unavailable)",
                    FileSystem = "Unknown",
                    TotalBytes = -1,
                    AvailableBytes = -1
                });
            }
        }

        return drives;
    }

    public List<DriveInfoModel> GetAllFixedAndRemovableDrives()
    {
        var drives = new List<DriveInfoModel>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady)
                continue;

            try
            {
                var model = new DriveInfoModel
                {
                    DriveLetter = drive.Name.TrimEnd('\\'),
                    VolumeLabel = GetSafeVolumeLabel(drive),
                    FileSystem = drive.DriveFormat ?? "Unknown",
                    TotalBytes = GetSafeTotalSize(drive),
                    AvailableBytes = GetSafeAvailableSize(drive),
                    VolumeGuidPath = VolumeMappingService.GetVolumeGuidPath(drive.Name)
                };

                drives.Add(model);
            }
            catch
            {
                // Skip inaccessible drives.
            }
        }

        return drives;
    }

    private static string GetSafeVolumeLabel(DriveInfo drive)
    {
        try { return drive.VolumeLabel; }
        catch { return string.Empty; }
    }

    private static long GetSafeTotalSize(DriveInfo drive)
    {
        try { return drive.TotalSize; }
        catch { return -1; }
    }

    private static long GetSafeAvailableSize(DriveInfo drive)
    {
        try { return drive.TotalFreeSpace; }
        catch { return -1; }
    }
}
