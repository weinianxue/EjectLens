using EjectLens.Models;
using EjectLens.Services;
using Xunit;

namespace EjectLens.Tests;

public class EventParserTests
{
    [Fact]
    public void ParseFromMessage_Sample1_ExtractsAppPathAndPid()
    {
        var message =
            "The application \\Device\\HarddiskVolume3\\Windows\\System32\\notepad.exe " +
            "with process id 1234 stopped the removal or ejection for the device " +
            "USB\\VID_0781&PID_5591\\0123456789ABCDEF";

        var result = EventParser.ParseFromMessage(message, new DateTime(2025, 6, 1, 12, 0, 0));

        Assert.NotNull(result);
        Assert.Equal(1234, result.ProcessId);
        Assert.Contains("notepad.exe", result.ApplicationPath);
        Assert.Contains("USB\\VID_0781", result.DeviceInstanceId);
        Assert.True(result.IsParsed);
    }

    [Fact]
    public void ParseFromMessage_Sample2_ExtractsCommandLine()
    {
        var message =
            "Application C:\\Program Files\\MyApp\\myapp.exe with process id 5678 " +
            "command line: myapp.exe --sync --verbose stopped the removal " +
            "for device SCSI\\Disk&Ven_Seagate&Prod_Expansion\\000000";

        var result = EventParser.ParseFromMessage(message, new DateTime(2025, 6, 1, 13, 0, 0));

        Assert.NotNull(result);
        Assert.Equal(5678, result.ProcessId);
        Assert.Contains("myapp.exe", result.ApplicationPath);
        Assert.Contains("SCSI", result.DeviceInstanceId);
        Assert.True(result.IsParsed);
    }

    [Fact]
    public void ParseFromMessage_Sample3_ParsesPartialData()
    {
        var message =
            "A process with process id 9999 has blocked device removal. " +
            "Device instance: USB\\VID_0951&PID_1666\\ABCDEF123456";

        var result = EventParser.ParseFromMessage(message, new DateTime(2025, 6, 1, 14, 0, 0));

        Assert.NotNull(result);
        Assert.Equal(9999, result.ProcessId);
        Assert.Contains("USB\\VID_0951", result.DeviceInstanceId);
        Assert.True(result.IsParsed);
    }

    [Fact]
    public void ParseFromMessage_EmptyMessage_ReturnsUnparsed()
    {
        var result = EventParser.ParseFromMessage("", DateTime.Now);

        Assert.NotNull(result);
        Assert.False(result.IsParsed);
        Assert.Equal(0, result.ProcessId);
    }

    [Fact]
    public void ParseFromMessage_GarbageMessage_ReturnsUnparsed()
    {
        var result = EventParser.ParseFromMessage(
            "Something went wrong with the operation.", DateTime.Now);

        Assert.NotNull(result);
        Assert.False(result.IsParsed);
    }

    [Fact]
    public void ParseFromMessage_DevicePatternRecognition()
    {
        var message =
            "Process explorer.exe (PID 4096) prevented removal of " +
            "device IDE\\DiskST2000DM008-2FR102\\___________. " +
            "CommandLine: C:\\Windows\\explorer.exe /factory,{guid}";

        var result = EventParser.ParseFromMessage(message, DateTime.Now);

        Assert.NotNull(result);
        Assert.Equal(4096, result.ProcessId);
        Assert.Contains("explorer.exe", result.ApplicationPath);
        Assert.Contains("IDE", result.DeviceInstanceId);
        Assert.True(result.IsParsed);
    }

    [Fact]
    public void ParseFromMessage_HandlesDifferentPidFormats()
    {
        var r1 = EventParser.ParseFromMessage(
            "process id: 4444 blocked device USB\\VID_0000&PID_0000\\123",
            DateTime.Now);
        Assert.Equal(4444, r1.ProcessId);

        var r2 = EventParser.ParseFromMessage(
            "PID: 5555 has blocked removal of device STORAGE\\Volume\\{guid}",
            DateTime.Now);
        Assert.Equal(5555, r2.ProcessId);

        var r3 = EventParser.ParseFromMessage(
            "process identifier 6666 prevented ejection",
            DateTime.Now);
        Assert.Equal(6666, r3.ProcessId);
    }

    [Fact]
    public void EventParser_ExtractsProcessName()
    {
        var evt = new EjectBlockEvent
        {
            ApplicationPath = @"C:\Windows\System32\notepad.exe",
            ProcessId = 1234
        };

        Assert.Equal("notepad.exe", evt.ProcessName);
    }

    [Fact]
    public void EventParser_ProcessNameFallbackToCommandLine()
    {
        var evt = new EjectBlockEvent
        {
            ApplicationPath = "",
            CommandLine = @"C:\Program Files\Test\app.exe --flag",
            ProcessId = 1234
        };

        Assert.Equal("app.exe", evt.ProcessName);
    }

    [Fact]
    public void EventParser_ProcessNameUnknown()
    {
        var evt = new EjectBlockEvent
        {
            ApplicationPath = "",
            CommandLine = "",
            ProcessId = 0
        };

        Assert.Equal("(unknown)", evt.ProcessName);
    }
}
