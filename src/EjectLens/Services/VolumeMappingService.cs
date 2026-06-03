using System.Diagnostics;
using System.Text.RegularExpressions;
using EjectLens.Native;

namespace EjectLens.Services;

/// <summary>
/// Maps drive letters to volume GUID paths.
/// Uses GetVolumeNameForVolumeMountPoint via P/Invoke,
/// falling back to parsing mountvol.exe output.
/// </summary>
public static class VolumeMappingService
{
    /// <summary>
    /// Given a drive root path like "D:\", returns the volume GUID path
    /// like "\\?\Volume{xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}\".
    /// Returns null if the volume cannot be resolved.
    /// </summary>
    public static string? GetVolumeGuidPath(string driveRoot)
    {
        // Ensure the path ends with a backslash (required by the API).
        if (!driveRoot.EndsWith('\\'))
            driveRoot += '\\';

        // Primary method: GetVolumeNameForVolumeMountPoint P/Invoke.
        var buffer = new char[260];
        if (VolumeNative.GetVolumeNameForVolumeMountPoint(driveRoot, buffer, buffer.Length))
        {
            var result = new string(buffer).TrimEnd('\0');
            if (!string.IsNullOrEmpty(result))
                return result;
        }

        // Fallback: parse mountvol.exe output.
        return GetVolumeGuidFromMountvol(driveRoot);
    }

    /// <summary>
    /// Given a drive root, returns all known volume identifiers:
    /// GUID path, DOS device path variants, and the drive letter itself.
    /// These are used to fuzzy-match event log device references.
    /// </summary>
    public static List<string> GetVolumeIdentifiers(string driveRoot)
    {
        var ids = new List<string>();

        if (!driveRoot.EndsWith('\\'))
            driveRoot += '\\';

        // The drive letter itself.
        ids.Add(driveRoot.TrimEnd('\\'));

        // Volume GUID path.
        var guidPath = GetVolumeGuidPath(driveRoot);
        if (guidPath != null)
        {
            ids.Add(guidPath);
            // Also add the GUID without trailing backslash.
            ids.Add(guidPath.TrimEnd('\\'));

            // Extract the bare GUID for matching.
            var match = Regex.Match(guidPath, @"\{[a-fA-F0-9\-]+\}");
            if (match.Success)
                ids.Add(match.Value);
        }

        return ids;
    }

    private static string? GetVolumeGuidFromMountvol(string driveRoot)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "mountvol.exe",
                Arguments = driveRoot.TrimEnd('\\'),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            if (process.ExitCode != 0) return null;

            // mountvol output for a specific drive looks like:
            // "\\?\Volume{xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}\"
            var match = Regex.Match(output, @"\\\\\?\\Volume\{[a-fA-F0-9\-]+\}\\?");
            return match.Success ? match.Value.TrimEnd('\\') : null;
        }
        catch
        {
            return null;
        }
    }
}
