using System.Runtime.InteropServices;

namespace EjectLens.Native;

/// <summary>
/// P/Invoke declarations for Windows Configuration Manager and Device I/O APIs
/// used for safe device ejection with proper parent-device targeting.
/// </summary>
internal static class DeviceEjectNative
{
    public const int CR_SUCCESS = 0x00;
    public const int CR_REMOVE_VETOED = 0x11;
    public const int CR_NO_SUCH_DEVNODE = 0x0D;
    public const int CR_ACCESS_DENIED = 0x2A;
    public const int CR_FAILURE = 0x15;
    public const int CR_INVALID_DEVNODE = 0x05;
    public const int CR_INVALID_DEVICE_ID = 0x20;
    public const int CR_CALL_NOT_IMPLEMENTED = 0x2B;
    public const int MAX_DEVICE_ID_LEN = 260;

    public enum PNP_VETO_TYPE
    {
        Ok = 0, Unknown = 1, LegacyDevice = 2, PendingClose = 3,
        WindowsApp = 4, WindowsService = 5, OutstandingOpen = 6,
        Device = 7, Driver = 8, IllegalDeviceRequest = 9,
        InsufficientPower = 10, NonDisableable = 11,
        LegacyDriver = 12, InsufficientRights = 13
    }

    public const uint FSCTL_LOCK_VOLUME = 0x00090018;
    public const uint IOCTL_STORAGE_EJECT_MEDIA = 0x002D4808;
    public const uint GENERIC_READ = 0x80000000;
    public const uint GENERIC_WRITE = 0x40000000;
    public const uint FILE_SHARE_READ = 0x00000001;
    public const uint FILE_SHARE_WRITE = 0x00000002;
    public const uint OPEN_EXISTING = 3;
    public const uint FILE_ATTRIBUTE_NORMAL = 0x80;
    public const uint FILE_FLAG_NO_BUFFERING = 0x20000000;

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int CM_Locate_DevNodeW(out int pdnDevInst, string pDeviceID, int ulFlags);

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int CM_Get_Parent(out int pdnDevInst, int dnDevInst, int ulFlags);

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int CM_Get_Child(out int pdnDevInst, int dnDevInst, int ulFlags);

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int CM_Get_Sibling(out int pdnDevInst, int dnDevInst, int ulFlags);

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int CM_Get_Device_IDW(int dnDevInst, [Out] char[] Buffer, int BufferLen, int ulFlags);

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int CM_Get_DevNode_Status(out int pulStatus, out int pulProblemNumber, int dnDevInst, int ulFlags);

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int CM_Request_Device_EjectW(int dnDevInst, out PNP_VETO_TYPE pVetoType, [Out] char[] pszVetoName, int ulNameLength, int ulFlags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr CreateFileW(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool FlushFileBuffers(IntPtr hFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode, IntPtr lpInBuffer, uint nInBufferSize, IntPtr lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    public static string GetReturnCodeName(int code) => code switch
    {
        CR_SUCCESS => "CR_SUCCESS",
        CR_REMOVE_VETOED => "CR_REMOVE_VETOED",
        CR_ACCESS_DENIED => "CR_ACCESS_DENIED",
        CR_NO_SUCH_DEVNODE => "CR_NO_SUCH_DEVNODE",
        CR_FAILURE => "CR_FAILURE",
        CR_INVALID_DEVNODE => "CR_INVALID_DEVNODE",
        CR_INVALID_DEVICE_ID => "CR_INVALID_DEVICE_ID",
        CR_CALL_NOT_IMPLEMENTED => "CR_CALL_NOT_IMPLEMENTED",
        _ => $"UNKNOWN (0x{code:X})"
    };

    public static string GetVetoAdvice(PNP_VETO_TYPE vetoType) => vetoType switch
    {
        PNP_VETO_TYPE.Driver =>
            "The storage driver rejected the request. Close all files and " +
            "applications using the drive, wait a few seconds, then try again. " +
            "Running EjectLens as administrator may help for some devices.",
        PNP_VETO_TYPE.OutstandingOpen =>
            "A process still has an open handle on the device. Use Scan " +
            "Selected Path or check Event Log for blocking processes.",
        PNP_VETO_TYPE.WindowsApp =>
            "A Windows application is preventing removal. Close Explorer " +
            "windows and any apps that may be accessing the drive.",
        PNP_VETO_TYPE.WindowsService =>
            "A Windows service is preventing removal. This may resolve " +
            "itself after a few seconds.",
        PNP_VETO_TYPE.InsufficientRights =>
            "Administrator privileges are required. Run EjectLens as " +
            "administrator and try again.",
        PNP_VETO_TYPE.Device =>
            "The device itself rejected the removal request. Try unplugging " +
            "after ensuring no files are open.",
        PNP_VETO_TYPE.PendingClose =>
            "A process has pending I/O on the device. Wait a moment and try again.",
        PNP_VETO_TYPE.InsufficientPower =>
            "The device cannot be removed due to power constraints.",
        _ => "Windows rejected the request. The veto reason was not detailed."
    };
}
