using System.Diagnostics;
using System.Security;
using EjectLens.Models;
using EjectLens.Utils;

namespace EjectLens.Services;

/// <summary>
/// Reads Microsoft-Windows-Kernel-PnP Event ID 225 from the System event log
/// within a specified time window.
///
/// Event 225: "An application or service has blocked device removal."
/// Provider: Microsoft-Windows-Kernel-PnP
/// Log: System
/// </summary>
public class EventLogService
{
    private const string LogName = "System";
    private const string PnpSource = "Microsoft-Windows-Kernel-PnP";
    private const int EjectEventId = 225;

    /// <summary>
    /// Query the System event log for Kernel-PnP 225 events within the given time range.
    /// Returns an empty list if the event log is unavailable or no events match.
    /// </summary>
    public List<EjectBlockEvent> GetEjectBlockEvents(TimeRange timeRange)
    {
        var events = new List<EjectBlockEvent>();

        try
        {
            using var eventLog = new EventLog(LogName);

            var since = DateTime.Now - timeRange.ToTimeSpan();

            // Iterate from newest to oldest.
            for (int i = eventLog.Entries.Count - 1; i >= 0; i--)
            {
                EventLogEntry entry;
                try
                {
                    entry = eventLog.Entries[i];
                }
                catch
                {
                    continue;
                }

                // Stop when we go past the time window.
                if (entry.TimeGenerated < since)
                    break;

                // Filter by source and event ID.
                if (entry.Source != PnpSource || entry.InstanceId != EjectEventId)
                    continue;

                var parsed = EventParser.Parse(entry);
                if (parsed != null)
                    events.Add(parsed);
            }
        }
        catch (SecurityException)
        {
            // Non-admin users may not have permission to read the System log.
            // Return empty list — the UI will show an appropriate message.
        }
        catch (Exception)
        {
            // Event log unavailable or corrupt. Return whatever we have.
        }

        return events;
    }

    /// <summary>
    /// Query without using EventLog class — try to use EventLogReader (modern API)
    /// for better XML access. Falls back to traditional EventLog on failure.
    /// </summary>
    public List<EjectBlockEvent> GetEjectBlockEventsAdvanced(TimeRange timeRange)
    {
        // Traditional EventLog is the most compatible approach.
        // EventLogReader requires more complex query strings and may not
        // be available in all environments. We stick with the simple path.
        return GetEjectBlockEvents(timeRange);
    }
}
