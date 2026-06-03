using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;
using EjectLens.Models;

namespace EjectLens.Services;

/// <summary>
/// Parses Microsoft-Windows-Kernel-PnP Event ID 225 entries.
///
/// Event 225 is raised when a process blocks device removal.
/// The event contains UserData XML with:
///   - ProcessId: the blocking PID
///   - ApplicationPath: the executable path
///   - CommandLine: the command line of the process
///   - DeviceInstanceId: the affected device hardware ID
///
/// If XML parsing fails or fields are missing, falls back to
/// regex extraction from the event's formatted message.
/// </summary>
public static class EventParser
{
    // XML namespace used in Kernel-PnP events.
    private const string PnpNs = "http://manifests.microsoft.com/win/2004/08/windows/kernel-pnp";

    /// <summary>
    /// Parse a single EventLogEntry into an EjectBlockEvent.
    /// Returns null if the entry is not an Event ID 225.
    /// </summary>
    public static EjectBlockEvent? Parse(EventLogEntry entry)
    {
        if (entry.InstanceId != 225)
            return null;

        var eventTime = entry.TimeGenerated;
        var rawMessage = entry.Message ?? string.Empty;

        // Try XML parsing first via the event data.
        var xmlResult = TryParseFromEventData(entry, eventTime);
        if (xmlResult != null && xmlResult.IsParsed)
            return xmlResult;

        // Fallback: regex extraction from the formatted message.
        var regexResult = ParseFromMessage(rawMessage, eventTime);
        return regexResult;
    }

    /// <summary>
    /// Parse from the raw event XML (EventLogEntry exposes data as replacement strings,
    /// but in newer .NET we may be able to access the full event XML via EventLogRecord).
    /// This method tries to extract structured fields from the event's UserData section.
    /// </summary>
    public static EjectBlockEvent? TryParseFromXml(string eventXml, DateTime eventTime)
    {
        if (string.IsNullOrEmpty(eventXml))
            return null;

        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(eventXml);

            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("e", "http://schemas.microsoft.com/win/2004/08/events/event");
            nsmgr.AddNamespace("pnp", PnpNs);

            // Extract from UserData / PkPnPDevInit.
            var processIdNode = doc.SelectSingleNode("//pnp:PkPnPDevInit/pnp:ProcessId", nsmgr)
                                ?? doc.SelectSingleNode("//pnp:ProcessId", nsmgr);
            var appPathNode = doc.SelectSingleNode("//pnp:PkPnPDevInit/pnp:ApplicationPath", nsmgr)
                              ?? doc.SelectSingleNode("//pnp:ApplicationPath", nsmgr);
            var cmdLineNode = doc.SelectSingleNode("//pnp:PkPnPDevInit/pnp:CommandLine", nsmgr)
                              ?? doc.SelectSingleNode("//pnp:CommandLine", nsmgr);
            var deviceIdNode = doc.SelectSingleNode("//pnp:PkPnPDevInit/pnp:DeviceInstanceId", nsmgr)
                               ?? doc.SelectSingleNode("//pnp:DeviceInstanceId", nsmgr);

            bool hasAnyField = processIdNode != null || appPathNode != null
                               || cmdLineNode != null || deviceIdNode != null;

            return new EjectBlockEvent
            {
                EventTime = eventTime,
                ProcessId = int.TryParse(processIdNode?.InnerText, out var pid) ? pid : 0,
                ApplicationPath = appPathNode?.InnerText ?? string.Empty,
                CommandLine = cmdLineNode?.InnerText ?? string.Empty,
                DeviceInstanceId = deviceIdNode?.InnerText ?? string.Empty,
                RawMessageSummary = TruncateMessage(string.Empty),
                IsParsed = hasAnyField
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Fallback: parse Event ID 225 message text using regex patterns.
    /// Event 225 messages vary by Windows build. We handle multiple known formats.
    /// </summary>
    public static EjectBlockEvent ParseFromMessage(string message, DateTime eventTime)
    {
        bool parsed = false;
        int pid = 0;
        string appPath = string.Empty;
        string cmdLine = string.Empty;
        string deviceId = string.Empty;

        // Pattern 1: Drive-letter path: "Application C:\path\to\app.exe..."
        var appMatch = Regex.Match(message,
            @"Application\s+['""]?(?<path>[A-Za-z]:\\[^\r\n<>""]{1,300}\.exe)",
            RegexOptions.IgnoreCase);
        if (appMatch.Success)
        {
            appPath = appMatch.Groups["path"].Value;
            parsed = true;
        }

        // Pattern 1b: NT device path: "\Device\HarddiskVolumeX\path\to\app.exe"
        if (string.IsNullOrEmpty(appPath))
        {
            var devMatch = Regex.Match(message,
                @"Application\s+(?<path>\\Device\\[^\s\r\n]{1,300}\.exe)",
                RegexOptions.IgnoreCase);
            if (devMatch.Success)
            {
                appPath = devMatch.Groups["path"].Value;
                parsed = true;
            }
        }

        // Pattern 1c: Volume GUID path: "\\?\Volume{guid}\path\to\app.exe"
        if (string.IsNullOrEmpty(appPath))
        {
            var volMatch = Regex.Match(message,
                @"Application\s+(?<path>\\\\\?\\Volume\{[a-fA-F0-9\-]{36}\}\\[^\s\r\n]{1,300}\.exe)",
                RegexOptions.IgnoreCase);
            if (volMatch.Success)
            {
                appPath = volMatch.Groups["path"].Value;
                parsed = true;
            }
        }

        // Pattern 1d: "Process explorer.exe (PID XXXX)" without Application prefix.
        if (string.IsNullOrEmpty(appPath))
        {
            var procMatch = Regex.Match(message,
                @"Process\s+(?<name>[^\s\r\n]{1,100}\.exe)\s*\(PID\s+\d+\)",
                RegexOptions.IgnoreCase);
            if (procMatch.Success)
            {
                appPath = procMatch.Groups["name"].Value;
                parsed = true;
            }
        }

        // Pattern: "process id 1234" or "ProcessId: 1234" or "PID: 1234"
        var pidMatch = Regex.Match(message,
            @"(?:process\s*(?:id|identifier)?\s*[:\s]+|PID\s*[:\s]*)(?<pid>\d+)",
            RegexOptions.IgnoreCase);
        if (pidMatch.Success && int.TryParse(pidMatch.Groups["pid"].Value, out var parsedPid))
        {
            pid = parsedPid;
            parsed = true;
        }

        // Pattern: command line extraction.
        var cmdMatch = Regex.Match(message,
            @"(?:command\s*line|commandline|cmdline)\s*[:\s]+(?<cmd>[^\r\n]{1,500})",
            RegexOptions.IgnoreCase);
        if (cmdMatch.Success)
        {
            cmdLine = cmdMatch.Groups["cmd"].Value.Trim();
            parsed = true;
        }

        // Pattern: device instance / hardware ID.
        var devIdMatch = Regex.Match(message,
            @"(?:device\s*(?:instance\s*)?(?:id|path)|hardware\s*id|DeviceInstanceId)\s*[:\s]+(?<dev>[^\r\n]{1,300})",
            RegexOptions.IgnoreCase);
        if (devIdMatch.Success)
        {
            deviceId = devIdMatch.Groups["dev"].Value.Trim();
            parsed = true;
        }

        // Also look for common device ID patterns (USB\VID_, SCSI\, IDE\, etc.).
        if (string.IsNullOrEmpty(deviceId))
        {
            var hwMatch = Regex.Match(message,
                @"(?<dev>(?:USB|SCSI|IDE|PCI|ACPI|STORAGE|SWD|HID|ROOT|DISPLAY)\\[^\s\r\n]{1,300})",
                RegexOptions.IgnoreCase);
            if (hwMatch.Success)
            {
                deviceId = hwMatch.Groups["dev"].Value;
                parsed = true;
            }
        }

        return new EjectBlockEvent
        {
            EventTime = eventTime,
            ProcessId = pid,
            ApplicationPath = appPath,
            CommandLine = cmdLine,
            DeviceInstanceId = deviceId,
            RawMessageSummary = TruncateMessage(message),
            IsParsed = parsed
        };
    }

    /// <summary>
    /// Attempt to parse structured fields from the EventLogEntry's data
    /// (the replacement strings stored in the event log).
    /// For Kernel-PnP 225, the data array typically contains the
    /// structured fields at specific indices.
    /// </summary>
    private static EjectBlockEvent? TryParseFromEventData(EventLogEntry entry, DateTime eventTime)
    {
        try
        {
            // EventLogEntry.Data contains the raw binary data.
            // For modern event logs, the replacement strings may be accessible.
            // We try to get them from the entry properties.
            var data = entry.Data;
            if (data == null || data.Length == 0)
                return null;

            // The binary data contains Unicode strings. Try to extract them.
            // This is a best-effort extraction from the raw data bytes.
            var text = System.Text.Encoding.Unicode.GetString(data);
            if (string.IsNullOrEmpty(text))
                return null;

            // Try to extract PID and paths from the data blob.
            int pid = 0;
            string appPath = string.Empty;
            string cmdLine = string.Empty;
            string deviceId = string.Empty;

            // Look for the structured fields in the data text.
            var pidMatch = Regex.Match(text, @"\b(\d{2,8})\b");
            if (pidMatch.Success)
                int.TryParse(pidMatch.Groups[1].Value, out pid);

            var exeMatch = Regex.Match(text, @"([A-Za-z]:\\[^\0]+\.exe)");
            if (exeMatch.Success)
                appPath = exeMatch.Groups[1].Value;

            var devMatch = Regex.Match(text, @"((?:USB|SCSI|IDE|STORAGE)\\[^\0]+?\{[a-fA-F0-9\-]+\}[^\0]*)");
            if (devMatch.Success)
                deviceId = devMatch.Groups[1].Value;

            bool hasFields = pid > 0 || !string.IsNullOrEmpty(appPath) || !string.IsNullOrEmpty(deviceId);

            return new EjectBlockEvent
            {
                EventTime = eventTime,
                ProcessId = pid,
                ApplicationPath = appPath,
                CommandLine = cmdLine,
                DeviceInstanceId = deviceId,
                RawMessageSummary = TruncateMessage(entry.Message ?? string.Empty),
                IsParsed = hasFields
            };
        }
        catch
        {
            return null;
        }
    }

    private static string TruncateMessage(string msg)
    {
        if (string.IsNullOrEmpty(msg))
            return "(empty message)";
        return msg.Length > 300 ? msg[..300] + "..." : msg;
    }
}
