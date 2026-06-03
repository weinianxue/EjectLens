# EjectLens

Find out which process is preventing Windows from safely ejecting your USB drive.

EjectLens reads the Windows System event log to surface the processes that blocked a device ejection, then presents the findings in a sortable, color-coded table. It also includes a lightweight file-lock scanner that uses the Restart Manager API to detect open handles on the drive.

## Features

- Lists removable drives with volume label, file system, and capacity.
- Reads Microsoft-Windows-Kernel-PnP Event ID 225 from the System event log.
- Extracts the blocking process name, PID, command line, and affected device hardware ID.
- Color-codes events as **Matched**, **Possibly related**, or **Unmatched** against a selected drive.
- Shows whether each blocking process is still running or has already exited.
- Scans a selected folder for open file handles (Restart Manager, limited to 300 files).
- Exports a diagnostic report as plain text, copied to clipboard or saved as a `.txt` file.
- Quick-launch button for Task Manager.

## Safety model

EjectLens is read-only. It does not:

- Terminate processes
- Force-eject devices
- Modify the registry
- Install services or drivers
- Start with Windows
- Connect to the network
- Collect or send telemetry

## Requirements

- Windows 10 or Windows 11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

## Download

Release packages are available from [GitHub Releases](https://github.com/weinianxue/EjectLens/releases).

## Build from source

```powershell
git clone https://github.com/weinianxue/EjectLens.git
cd EjectLens
dotnet restore
dotnet build EjectLens.sln -c Release
dotnet test EjectLens.sln -c Release
```

To create a framework-dependent publish:

```powershell
dotnet publish src/EjectLens/EjectLens.csproj -c Release -r win-x64 --self-contained false
```

## Usage

1. Launch EjectLens.
2. Select a removable drive from the dropdown.
3. Pick a time range and click **Scan Events**.
4. Review the events table. Matched events are highlighted in green, possibly related events in yellow.
5. Click a row to see full details and advisory notes for well-known system processes.
6. Optionally scan a folder on the drive for open file handles.
7. Use **Copy Report** or **Save Report** to export findings.

## Sample report

A sanitized example of the diagnostic output is in [docs/sample-report.txt](docs/sample-report.txt).

## Screenshots

Screenshots will be added after capturing the application on Windows. See [docs/screenshots/README.md](docs/screenshots/README.md).

## Limitations

- Event ID 225 must be present in the System event log for the tool to find blocking processes. If Windows did not log the event, EjectLens cannot identify what blocked the eject. This varies by Windows version and device driver.
- Reading the System event log may require administrator privileges on some systems.
- The Restart Manager file-lock scanner is capped at 300 files per folder to avoid performance problems.
- Volume-to-drive matching uses heuristic string comparison and may not be exact.

## Privacy

EjectLens runs entirely on your machine. No data is collected, stored externally, or sent over the network. Reports redact user profile paths with `%USERPROFILE%` and similar tokens.

## Roadmap

- Event ID 226 (device removal timeout) analysis.
- Per-file handle enumeration beyond Restart Manager.
- Localization.

## License

MIT. See [LICENSE](LICENSE).
