using System.Management;
using EjectLens.Models;
using EjectLens.Native;

namespace EjectLens.Services;

/// <summary>
/// Requests Windows to safely eject a removable drive.
///
/// Uses the Windows Configuration Manager API (CM_Request_Device_EjectW)
/// after resolving a drive letter to a PNPDeviceID via WMI.
/// </summary>
public class DeviceEjectService : IDeviceEjectService
{
    public EjectResult EjectDrive(string driveLetter)
    {
        if (string.IsNullOrEmpty(driveLetter))
            return EjectResult.Failed(driveLetter, "No drive selected.");

        var pnpDeviceId = ResolvePnpDeviceId(driveLetter);
        if (pnpDeviceId == null)
            return EjectResult.Failed(driveLetter,
                "Could not resolve the selected drive to a removable device instance.");

        return RequestEject(driveLetter, pnpDeviceId);
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

    public virtual EjectResult RequestEject(string driveLetter, string pnpDeviceId)
    {
        try
        {
            int result = DeviceEjectNative.CM_Locate_DevNodeW(
                out int devInst, pnpDeviceId, 0);

            if (result != DeviceEjectNative.CR_SUCCESS)
            {
                return new EjectResult
                {
                    Success = false,
                    DriveLetter = driveLetter,
                    PnpDeviceId = pnpDeviceId,
                    NativeReturnCode = result,
                    Message = $"Could not locate device node. Return code: {result}."
                };
            }

            var vetoNameBuffer = new char[256];
            var ejectResult = DeviceEjectNative.CM_Request_Device_EjectW(
                devInst,
                out DeviceEjectNative.PNP_VETO_TYPE vetoType,
                vetoNameBuffer,
                vetoNameBuffer.Length,
                0);

            if (ejectResult == DeviceEjectNative.CR_SUCCESS)
            {
                return EjectResult.Ok(driveLetter, pnpDeviceId);
            }

            var vetoName = new string(vetoNameBuffer).TrimEnd('\0');

            return new EjectResult
            {
                Success = false,
                DriveLetter = driveLetter,
                PnpDeviceId = pnpDeviceId,
                NativeReturnCode = ejectResult,
                VetoType = vetoType.ToString(),
                VetoName = string.IsNullOrEmpty(vetoName) ? null : vetoName,
                Message = vetoType switch
                {
                    DeviceEjectNative.PNP_VETO_TYPE.OutstandingOpen =>
                        "The device has open handles. Close applications using files on the drive and try again.",
                    DeviceEjectNative.PNP_VETO_TYPE.WindowsApp =>
                        "A Windows application is preventing removal.",
                    DeviceEjectNative.PNP_VETO_TYPE.WindowsService =>
                        "A Windows service is preventing removal.",
                    DeviceEjectNative.PNP_VETO_TYPE.InsufficientRights =>
                        "Administrator privileges may be required to eject this device.",
                    _ => $"Windows rejected the eject request. Veto type: {vetoType}."
                }
            };
        }
        catch (Exception ex)
        {
            return EjectResult.Failed(driveLetter, $"Eject failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Interface for DeviceEjectService to support unit testing.
/// </summary>
public interface IDeviceEjectService
{
    EjectResult EjectDrive(string driveLetter);
    string? ResolvePnpDeviceId(string driveLetter);
    EjectResult RequestEject(string driveLetter, string pnpDeviceId);
}
