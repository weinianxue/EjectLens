using System.ComponentModel;
using EjectLens.Models;
using EjectLens.Native;

namespace EjectLens.Services;

/// <summary>
/// Wraps the Windows Restart Manager API to find processes that have
/// open handles to a specific file or set of files.
///
/// Designed for lightweight scanning only — we limit the number of
/// registered resources to avoid performance issues.
/// </summary>
public class RestartManagerService
{
    /// <summary>
    /// Maximum number of files to register with Restart Manager in a single session.
    /// </summary>
    public const int MaxFilesPerSession = 300;

    /// <summary>
    /// Scan a single file for lock-holding processes.
    /// Returns a list of processes that have open handles to the file.
    /// </summary>
    public List<LockingProcessInfo> FindLockingProcesses(string filePath)
    {
        return FindLockingProcesses([filePath]);
    }

    /// <summary>
    /// Scan a list of files for lock-holding processes.
    /// All file paths must be absolute.
    /// </summary>
    public List<LockingProcessInfo> FindLockingProcesses(string[] filePaths)
    {
        if (filePaths == null || filePaths.Length == 0)
            return [];

        // Limit to MaxFilesPerSession.
        if (filePaths.Length > MaxFilesPerSession)
            filePaths = filePaths[..MaxFilesPerSession];

        var results = new List<LockingProcessInfo>();

        uint sessionHandle = 0;
        try
        {
            var errorCode = RestartManagerNative.RmStartSession(
                out sessionHandle,
                dwSessionFlags: 0,
                strSessionKey: Guid.NewGuid().ToString());

            if (errorCode != RestartManagerNative.ERROR_SUCCESS)
                return results;

            errorCode = RestartManagerNative.RmRegisterResources(
                sessionHandle,
                (uint)filePaths.Length,
                filePaths,
                nApplications: 0,
                rgApplications: null,
                nServices: 0,
                rgsServiceNames: null);

            if (errorCode != RestartManagerNative.ERROR_SUCCESS)
                return results;

            uint nProcInfo = 0;
            uint nProcInfoNeeded = 0;
            uint rebootReasons = 0;

            errorCode = RestartManagerNative.RmGetList(
                sessionHandle,
                out nProcInfoNeeded,
                ref nProcInfo,
                rgAffectedApps: null,
                ref rebootReasons);

            if (errorCode == RestartManagerNative.ERROR_MORE_DATA && nProcInfoNeeded > 0)
            {
                nProcInfo = nProcInfoNeeded;
                var processInfos = new RestartManagerNative.RM_PROCESS_INFO[nProcInfo];

                errorCode = RestartManagerNative.RmGetList(
                    sessionHandle,
                    out nProcInfoNeeded,
                    ref nProcInfo,
                    processInfos,
                    ref rebootReasons);

                if (errorCode == RestartManagerNative.ERROR_SUCCESS)
                {
                    for (int i = 0; i < nProcInfo; i++)
                    {
                        var pi = processInfos[i];

                        results.Add(new LockingProcessInfo
                        {
                            ProcessId = pi.Process.dwProcessId,
                            ProcessName = Path.GetFileName(pi.strAppName),
                            ExecutablePath = pi.strAppName,
                            ApplicationType = pi.ApplicationType.ToString(),
                            ServiceShortName = string.IsNullOrEmpty(pi.strServiceShortName)
                                ? null
                                : pi.strServiceShortName
                        });
                    }
                }
            }
        }
        catch
        {
            // Restart Manager may fail for various reasons.
            // Return whatever partial results we have.
        }
        finally
        {
            if (sessionHandle != 0)
            {
                RestartManagerNative.RmEndSession(sessionHandle);
            }
        }

        return results;
    }

    /// <summary>
    /// Scan a folder (shallow) for locked files.
    /// Scans the first MaxFilesPerSession files found in the directory.
    /// Sorts by last write time descending to prioritize recently modified files.
    /// </summary>
    public List<LockingProcessInfo> ScanFolderForLocks(string folderPath, out int filesScanned)
    {
        filesScanned = 0;

        if (!Directory.Exists(folderPath))
            return [];

        try
        {
            // Get files in the directory, sorted by last write time (newest first).
            var dirInfo = new DirectoryInfo(folderPath);
            var files = dirInfo.GetFiles()
                .OrderByDescending(f => f.LastWriteTime)
                .Take(MaxFilesPerSession)
                .Select(f => f.FullName)
                .ToArray();

            filesScanned = files.Length;

            if (files.Length == 0)
                return [];

            return FindLockingProcesses(files);
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
        catch
        {
            return [];
        }
    }
}
