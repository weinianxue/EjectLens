using EjectLens.Models;
using EjectLens.Services;
using Xunit;

namespace EjectLens.Tests;

public class ReportServiceTests
{
    [Fact]
    public void GenerateReport_BasicStructure()
    {
        var model = new ReportModel
        {
            AppVersion = "v1.0.0",
            WindowsVersion = "Windows 11",
            SelectedDrive = "D: - USB Drive (NTFS, 32 GB)",
            ScanTime = new DateTime(2025, 6, 1, 12, 0, 0),
            TimeRangeDescription = "2 hours"
        };

        var report = ReportService.GenerateReport(model);

        Assert.Contains("EjectLens Diagnostic Report", report);
        Assert.Contains("v1.0.0", report);
        Assert.Contains("Windows 11", report);
        Assert.Contains("D: - USB Drive", report);
        Assert.Contains("2025-06-01", report);
        Assert.Contains("2 hours", report);
    }

    [Fact]
    public void GenerateReport_IncludesMatchedEvents()
    {
        var model = new ReportModel
        {
            AppVersion = "v1.0.0",
            WindowsVersion = "Windows 10",
            SelectedDrive = "E:",
            ScanTime = DateTime.Now,
            TimeRangeDescription = "1 hour",
            MatchedEvents =
            [
                new EjectBlockEvent
                {
                    EventTime = new DateTime(2025, 6, 1, 11, 30, 0),
                    ProcessId = 1234,
                    ApplicationPath = @"C:\Program Files\App\app.exe",
                    CommandLine = "app.exe --sync",
                    DeviceInstanceId = @"USB\VID_0781&PID_5591\ABC123",
                    IsProcessRunning = true,
                    MatchStatus = MatchStatus.Matched,
                    IsParsed = true
                }
            ]
        };

        var report = ReportService.GenerateReport(model);

        Assert.Contains("Matched Events", report);
        Assert.Contains("app.exe", report);
        Assert.Contains("1234", report);
        Assert.Contains("running", report);
        Assert.Contains("USB\\VID_0781", report);
    }

    [Fact]
    public void GenerateReport_IncludesPossiblyRelatedEvents()
    {
        var model = new ReportModel
        {
            AppVersion = "v1.0.0",
            WindowsVersion = "Windows 11",
            SelectedDrive = "F:",
            ScanTime = DateTime.Now,
            TimeRangeDescription = "2 hours",
            PossiblyRelatedEvents =
            [
                new EjectBlockEvent
                {
                    EventTime = DateTime.Now,
                    ProcessId = 4096,
                    ApplicationPath = @"C:\Windows\explorer.exe",
                    DeviceInstanceId = @"USB\VID_0000&PID_0000\XYZ",
                    IsProcessRunning = true,
                    MatchStatus = MatchStatus.PossiblyRelated,
                    IsParsed = true
                }
            ]
        };

        var report = ReportService.GenerateReport(model);

        Assert.Contains("Possibly Related Events", report);
        Assert.Contains("explorer.exe", report);
    }

    [Fact]
    public void GenerateReport_ShowsEmptySectionsCleanly()
    {
        var model = new ReportModel
        {
            AppVersion = "v1.0.0",
            WindowsVersion = "Windows 10",
            SelectedDrive = "G:",
            ScanTime = DateTime.Now,
            TimeRangeDescription = "15 minutes",
            MatchedEvents = [],
            PossiblyRelatedEvents = [],
            UnmatchedEvents = []
        };

        var report = ReportService.GenerateReport(model);

        Assert.Contains("(none)", report);
    }

    [Fact]
    public void GenerateReport_IncludesFileLockSection()
    {
        var model = new ReportModel
        {
            AppVersion = "v1.0.0",
            WindowsVersion = "Windows 11",
            SelectedDrive = "D:",
            ScanTime = DateTime.Now,
            TimeRangeDescription = "2 hours",
            FileLockScanPath = @"D:\test\file.txt",
            FileLockResults =
            [
                new LockingProcessInfo
                {
                    ProcessId = 7890,
                    ProcessName = "notepad.exe",
                    ExecutablePath = @"C:\Windows\System32\notepad.exe",
                    ApplicationType = "RmMainWindow"
                }
            ]
        };

        var report = ReportService.GenerateReport(model);

        Assert.Contains("File Lock Scan", report);
        Assert.Contains("notepad.exe", report);
        Assert.Contains("7890", report);
    }

    [Fact]
    public void GenerateReport_FileLockErrorShown()
    {
        var model = new ReportModel
        {
            AppVersion = "v1.0.0",
            WindowsVersion = "Windows 10",
            SelectedDrive = "E:",
            ScanTime = DateTime.Now,
            TimeRangeDescription = "1 hour",
            FileLockScanPath = @"E:\inaccessible",
            FileLockError = "Access denied."
        };

        var report = ReportService.GenerateReport(model);

        Assert.Contains("File Lock Scan", report);
        Assert.Contains("Access denied.", report);
    }

    [Fact]
    public void GenerateReport_ContainsFooter()
    {
        var model = new ReportModel
        {
            AppVersion = "v1.0.0",
            WindowsVersion = "Windows 11",
            SelectedDrive = "D:",
            ScanTime = DateTime.Now,
            TimeRangeDescription = "2 hours"
        };

        var report = ReportService.GenerateReport(model);

        Assert.Contains("read-only", report);
        Assert.Contains("No processes were terminated", report);
    }
}
