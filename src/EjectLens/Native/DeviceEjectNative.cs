using System.Runtime.InteropServices;

namespace EjectLens.Native;

/// <summary>
/// P/Invoke declarations for Windows Configuration Manager API
/// used to request safe device ejection.
///
/// CM_Request_Device_EjectW asks Windows to safely remove a device.
/// On success, the device can be physically disconnected.
/// On failure (veto), Windows returns a veto type and name explaining why.
/// </summary>
internal static class DeviceEjectNative
{
    public const int CR_SUCCESS = 0x00000000;
    public const int CR_REMOVE_VETOED = 0x00000011;

    public const int MAX_DEVICE_ID_LEN = 200;

    public enum PNP_VETO_TYPE
    {
        Ok = 0,
        Unknown = 1,
        LegacyDevice = 2,
        PendingClose = 3,
        WindowsApp = 4,
        WindowsService = 5,
        OutstandingOpen = 6,
        Device = 7,
        Driver = 8,
        IllegalDeviceRequest = 9,
        InsufficientPower = 10,
        NonDisableable = 11,
        LegacyDriver = 12,
        InsufficientRights = 13
    }

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int CM_Locate_DevNodeW(
        out int pdnDevInst,
        string pDeviceID,
        int ulFlags);

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int CM_Request_Device_EjectW(
        int dnDevInst,
        out PNP_VETO_TYPE pVetoType,
        [Out] char[] pszVetoName,
        int ulNameLength,
        int ulFlags);

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int CM_Get_DevNode_Status(
        out int pulStatus,
        out int pulProblemNumber,
        int dnDevInst,
        int ulFlags);
}
