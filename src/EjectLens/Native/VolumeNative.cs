using System.Runtime.InteropServices;

namespace EjectLens.Native;

/// <summary>
/// P/Invoke declarations for Windows Volume Management API.
/// </summary>
internal static class VolumeNative
{
    /// <summary>
    /// Retrieves the volume GUID path for a given volume mount point (e.g., "D:\").
    /// Returns a string like "\\?\Volume{xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}\".
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetVolumeNameForVolumeMountPoint(
        string lpszVolumeMountPoint,
        [Out] char[] lpszVolumeName,
        int cchBufferLength);
}
