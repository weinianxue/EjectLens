using System.Management;
using System.Runtime.InteropServices;
using EjectLens.Models;
using EjectLens.Native;

namespace EjectLens.Services;

/// <summary>
/// Requests Windows to safely eject a removable drive.
///
/// The key insight: calling CM_Request_Device_EjectW on the raw
/// Win32_DiskDrive.PNPDeviceID (e.g. USBSTOR\DISK&VEN_...) often
/// returns Driver veto. Windows "Safely Remove Hardware" targets
/// the USB parent device node, not the storage child. This service
/// walks the device tree upward from the disk node, scores
/// candidates by likelihood of being the correct removable target,
/// and tries each in priority order.
/// </summary>
public class DeviceEjectService : IDeviceEjectService
{
    private const int MaxParentDepth = 6;

    public EjectResult EjectDrive(string driveLetter)
    {
        var drive = driveLetter.TrimEnd('\\').TrimEnd(':');
        if (string.IsNullOrEmpty(drive))
            return EjectResult.Failed(driveLetter, "No drive selected.");

        var diskPnpId = ResolvePnpDeviceId(drive);
        if (diskPnpId == null)
            return EjectResult.Failed(driveLetter,
                "Could not resolve the selected drive to a physical disk device.");

        int locResult = LocateDiskDevNode(diskPnpId, out int diskDevInst);

        var candidates = BuildCandidateList(diskDevInst, diskPnpId, driveLetter);

        if (locResult != DeviceEjectNative.CR_SUCCESS)
        {
            candidates.Insert(0, new EjectCandidate
            {
                DeviceInstanceId = diskPnpId,
                Level = "DiskDrive",
                IsLikelyRemovableTarget = false,
                WhySelected = "Direct disk PNPDeviceID (devnode not locatable)"
            });
        }

        foreach (var candidate in candidates)
        {
            candidate.WasTried = true;
            TryEjectCandidate(candidate);
            if (candidate.ReturnCode == DeviceEjectNative.CR_SUCCESS)
                return EjectResult.Ok(driveLetter, candidate);
        }

        var fallbackResult = TrySafeVolumeEject(driveLetter);

        var bestFailed = candidates
            .Where(c => c.WasTried)
            .MaxBy(c => c.IsLikelyRemovableTarget ? 1 : 0)
            ?? candidates.FirstOrDefault();

        var failedVetoType = bestFailed?.VetoType;
        var failedVetoName = bestFailed?.VetoName;
        var failedRc = bestFailed?.ReturnCode ?? -1;

        var failedMessage = fallbackResult != null
            ? $"All device eject attempts failed. Volume fallback: {fallbackResult}"
            : "All device eject attempts failed. No candidate accepted the eject request.";

        var suggestions = new List<string>();

        if (failedVetoType == "Driver")
            suggestions.Add("Driver veto — the storage driver rejected removal. "
                + "Close all files on the drive, wait a few seconds, and try again.");
        else if (failedVetoType == "OutstandingOpen")
            suggestions.Add("A process has open handles on the device. "
                + "Use Scan Selected Path or check for blocking processes.");
        else if (failedVetoType == "InsufficientRights")
            suggestions.Add("Run EjectLens as administrator for additional privileges.");
        else
            suggestions.Add("Close all applications using the drive and try again.");

        suggestions.Add("Try the Windows 'Safely Remove Hardware' tray icon as an alternative.");
        suggestions.Add("If the problem persists, restarting Windows will release all handles.");

        return new EjectResult
        {
            Success = false,
            DriveLetter = driveLetter,
            ReturnCode = failedRc,
            VetoType = failedVetoType,
            VetoName = failedVetoName,
            SelectedDeviceInstanceId = diskPnpId,
            Message = failedMessage,
            AttemptedCandidates = candidates,
            SuggestedNextSteps = suggestions,
            FallbackAttempted = fallbackResult != null,
            FallbackResult = fallbackResult
        };
    }

    public virtual int LocateDiskDevNode(string diskPnpId, out int diskDevInst)
    {
        return DeviceEjectNative.CM_Locate_DevNodeW(out diskDevInst, diskPnpId, 0);
    }

    public virtual List<EjectCandidate> BuildCandidateList(
        int diskDevInst, string diskPnpId, string driveLetter)
    {
        var candidates = new List<EjectCandidate>();

        candidates.Add(new EjectCandidate
        {
            DeviceInstanceId = diskPnpId,
            Level = "DiskDrive",
            IsLikelyRemovableTarget = false,
            WhySelected = "Direct disk device from Win32_DiskDrive.PNPDeviceID"
        });

        int current = diskDevInst;
        for (int depth = 1; depth <= MaxParentDepth; depth++)
        {
            int rc = DeviceEjectNative.CM_Get_Parent(out int parent, current, 0);
            if (rc != DeviceEjectNative.CR_SUCCESS) break;

            var parentId = GetDeviceId(parent);
            if (string.IsNullOrEmpty(parentId)) break;

            bool isUsb = parentId.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase);
            bool isUsbStor = parentId.StartsWith("USBSTOR\\", StringComparison.OrdinalIgnoreCase);

            candidates.Add(new EjectCandidate
            {
                DeviceInstanceId = parentId,
                Level = $"Parent{depth}",
                IsLikelyRemovableTarget = isUsb || isUsbStor,
                WhySelected = isUsb
                    ? "USB device parent — Windows normally ejects this node"
                    : isUsbStor
                        ? "USB storage parent — may accept eject"
                        : $"Ancestor at depth {depth}"
            });

            current = parent;
        }

        candidates.Sort((a, b) =>
        {
            int Score(EjectCandidate c)
            {
                if (c.Level.StartsWith("Parent") && c.IsLikelyRemovableTarget) return 100;
                if (c.Level.StartsWith("Parent")) return 60;
                if (c.Level == "DiskDrive") return 10;
                return 5;
            }
            return Score(b).CompareTo(Score(a));
        });

        return candidates;
    }

    public virtual void TryEjectCandidate(EjectCandidate candidate)
    {
        int devLoc = DeviceEjectNative.CM_Locate_DevNodeW(
            out int devInst, candidate.DeviceInstanceId, 0);

        if (devLoc != DeviceEjectNative.CR_SUCCESS)
        {
            candidate.ReturnCode = devLoc;
            return;
        }

        var vetoNameBuf = new char[256];
        int ejectRc = DeviceEjectNative.CM_Request_Device_EjectW(
            devInst,
            out DeviceEjectNative.PNP_VETO_TYPE vetoType,
            vetoNameBuf,
            vetoNameBuf.Length,
            0);

        candidate.ReturnCode = ejectRc;

        if (ejectRc != DeviceEjectNative.CR_SUCCESS)
        {
            candidate.VetoType = vetoType.ToString();
            var vn = new string(vetoNameBuf).TrimEnd('\0');
            candidate.VetoName = string.IsNullOrEmpty(vn) ? null : vn;
        }
    }

    public virtual string? TrySafeVolumeEject(string driveLetter)
    {
        var drive = driveLetter.TrimEnd('\\').TrimEnd(':');
        var volumePath = $"\\\\.\\{drive}:";

        IntPtr hVolume = IntPtr.Zero;
        try
        {
            hVolume = DeviceEjectNative.CreateFileW(
                volumePath,
                DeviceEjectNative.GENERIC_READ | DeviceEjectNative.GENERIC_WRITE,
                DeviceEjectNative.FILE_SHARE_READ | DeviceEjectNative.FILE_SHARE_WRITE,
                IntPtr.Zero,
                DeviceEjectNative.OPEN_EXISTING,
                DeviceEjectNative.FILE_FLAG_NO_BUFFERING | DeviceEjectNative.FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero);

            if (hVolume == IntPtr.Zero || hVolume == new IntPtr(-1))
            {
                int err = Marshal.GetLastWin32Error();
                return $"Cannot open volume {volumePath}. Error: {err}. " +
                       "Run as administrator if the drive is in use.";
            }

            DeviceEjectNative.FlushFileBuffers(hVolume);

            uint bytesReturned;
            bool locked = DeviceEjectNative.DeviceIoControl(
                hVolume, DeviceEjectNative.FSCTL_LOCK_VOLUME,
                IntPtr.Zero, 0, IntPtr.Zero, 0,
                out bytesReturned, IntPtr.Zero);

            if (!locked)
            {
                int err = Marshal.GetLastWin32Error();
                return $"Volume lock failed. Error: {err}. " +
                       "The volume is still in use by another process.";
            }

            bool ejected = DeviceEjectNative.DeviceIoControl(
                hVolume, DeviceEjectNative.IOCTL_STORAGE_EJECT_MEDIA,
                IntPtr.Zero, 0, IntPtr.Zero, 0,
                out bytesReturned, IntPtr.Zero);

            if (ejected)
                return "Volume lock and eject media succeeded.";

            int ejectErr = Marshal.GetLastWin32Error();
            return $"Volume locked but eject media failed. Error: {ejectErr}.";
        }
        catch (Exception ex)
        {
            return $"Volume fallback exception: {ex.Message}";
        }
        finally
        {
            if (hVolume != IntPtr.Zero && hVolume != new IntPtr(-1))
                DeviceEjectNative.CloseHandle(hVolume);
        }
    }

    public virtual string? ResolvePnpDeviceId(string driveLetter)
    {
        var drive = driveLetter.TrimEnd('\\').TrimEnd(':');

        try
        {
            using var searcher = new ManagementObjectSearcher(
                $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{drive}:'}} " +
                "WHERE AssocClass=Win32_LogicalDiskToPartition");

            foreach (var partition in searcher.Get())
            {
                using var diskSearcher = new ManagementObjectSearcher(
                    $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='" +
                    $"{partition["DeviceID"]}" +
                    $"'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition");

                foreach (var disk in diskSearcher.Get())
                {
                    var pnpId = disk["PNPDeviceID"] as string;
                    disk.Dispose();
                    partition.Dispose();
                    return pnpId;
                }

                partition.Dispose();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetDeviceId(int devInst)
    {
        var buf = new char[DeviceEjectNative.MAX_DEVICE_ID_LEN];
        int rc = DeviceEjectNative.CM_Get_Device_IDW(devInst, buf, buf.Length, 0);
        if (rc != DeviceEjectNative.CR_SUCCESS) return null;
        return new string(buf).TrimEnd('\0');
    }
}

public interface IDeviceEjectService
{
    EjectResult EjectDrive(string driveLetter);
    string? ResolvePnpDeviceId(string driveLetter);
    List<EjectCandidate> BuildCandidateList(int diskDevInst, string diskPnpId, string driveLetter);
    void TryEjectCandidate(EjectCandidate candidate);
    string? TrySafeVolumeEject(string driveLetter);
}
