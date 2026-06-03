# EjectLens

A Windows desktop diagnostic tool for troubleshooting "This device is currently in use" errors when safely removing USB drives and external hard disks.

## What It Does

EjectLens reads Windows system event logs to identify which processes blocked a device ejection, then presents the findings in a clear, filterable table. It also includes a lightweight file-lock scanner that uses the Windows Restart Manager API to detect processes holding open handles to files on the drive.

## Why It Exists

Windows can be vague about why a USB drive cannot be safely removed. The information is often buried in the Event Viewer, which most users never open. EjectLens surfaces this data in a focused, single-purpose window.

## Features

- Lists removable drives with volume details (label, file system, capacity).
- Reads Microsoft-Windows-Kernel-PnP Event ID 225 entries from the System event log.
- Parses event data to extract the blocking process name, PID, command line, and affected device.
- Maps events to a selected drive using volume GUID matching (with a mountvol fallback).
- Color-codes events as Matched, Possibly related, or Unmatched.
- Shows whether the blocking process is still running or has exited.
- Scans a selected folder for open file handles using the Restart Manager API.
- Exports a diagnostic report as plain text (clipboard or .txt file).
- One-click access to Task Manager.

## Safety Model

EjectLens is **read-only by design**.

- It does not terminate processes.
- It does not force-eject devices.
- It does not modify the registry.
- It does not install services or drivers.
- It does not start with Windows.
- It does not connect to the network.
- It does not collect or send telemetry.

The tool provides information. You decide what to do with it.

## Requirements

- Windows 10 or Windows 11
- .NET 8 Desktop Runtime

## Build from Source

```powershell
git clone https://github.com/weinianxue/EjectLens.git
cd EjectLens
dotnet restore
dotnet build EjectLens.sln -c Release
```

## Run

```powershell
dotnet run --project src/EjectLens/EjectLens.csproj
```

Or publish a self-contained executable:

```powershell
dotnet publish src/EjectLens/EjectLens.csproj -c Release -r win-x64 --self-contained false
```

## Run Tests

```powershell
dotnet test EjectLens.sln -c Release
```

## Usage

1. Launch EjectLens.
2. Select a removable drive from the dropdown.
3. Choose a time range and click **Scan Events**.
4. Review the events table — matched events are highlighted in green.
5. Click an event row to see full details, including advisory notes for well-known system processes.
6. Optionally scan a folder on the drive for open file handles.
7. Use **Copy Report** or **Save Report** to export findings.

## Limitations

- Event ID 225 must be present in the Windows System event log. If Windows did not log the eject failure with this event, EjectLens cannot identify the blocking process. This varies by Windows version and device driver.
- Reading the System event log may require administrator privileges on some systems.
- Process command-line retrieval is best-effort and may be unavailable without elevation.
- The Restart Manager file-lock scanner only checks the first 300 files in a folder to avoid performance issues.
- Volume-to-drive matching uses heuristic comparison and may not always be exact.

## Privacy

EjectLens runs entirely on your machine. No data is collected, stored externally, or transmitted over the network. Reports redact user profile paths by replacing them with `%USERPROFILE%` and similar environment variable tokens.

## Roadmap

- Support for Event ID 226 (device removal timeout) analysis.
- Improved driver-level handle enumeration.
- Localization.

## License

MIT. See [LICENSE](LICENSE).
