using EjectLens.Models;
using EjectLens.Services;
using Xunit;

namespace EjectLens.Tests;

public class DeviceEjectServiceTests
{
    private sealed class TestDeviceEjectService : DeviceEjectService
    {
        private readonly string? _pnpDeviceId;
        private readonly EjectResult _ejectResult;

        public TestDeviceEjectService(string? pnpDeviceId, EjectResult ejectResult)
        {
            _pnpDeviceId = pnpDeviceId;
            _ejectResult = ejectResult;
        }

        public override string? ResolvePnpDeviceId(string driveLetter)
            => _pnpDeviceId;

        public override EjectResult RequestEject(string driveLetter, string pnpDeviceId)
            => _ejectResult;
    }

    [Fact]
    public void EjectDrive_NoDriveSelected_ReturnsFailed()
    {
        var service = new TestDeviceEjectService(null, EjectResult.Failed("", ""));
        var result = service.EjectDrive("");
        Assert.False(result.Success);
        Assert.Contains("No drive selected", result.Message);
    }

    [Fact]
    public void EjectDrive_CannotResolvePnpDevice_ReturnsFailed()
    {
        var service = new TestDeviceEjectService(null, EjectResult.Failed("D:", ""));
        var result = service.EjectDrive("D:");
        Assert.False(result.Success);
        Assert.Contains("Could not resolve", result.Message);
    }

    [Fact]
    public void EjectDrive_Success_ReturnsOk()
    {
        var okResult = EjectResult.Ok("D:", @"USB\VID_0781&PID_5591\ABC123");
        var service = new TestDeviceEjectService(
            @"USB\VID_0781&PID_5591\ABC123", okResult);
        var result = service.EjectDrive("D:");
        Assert.True(result.Success);
        Assert.Equal("D:", result.DriveLetter);
        Assert.Contains("safely removed", result.Message);
    }

    [Fact]
    public void EjectDrive_Vetoed_ReturnsVetoInfo()
    {
        var vetoed = new EjectResult
        {
            Success = false,
            DriveLetter = "E:",
            PnpDeviceId = @"USB\VID_0000&PID_0000\XYZ",
            NativeReturnCode = 0x11,
            VetoType = "OutstandingOpen",
            VetoName = "notepad.exe",
            Message = "The device has open handles."
        };
        var service = new TestDeviceEjectService(
            @"USB\VID_0000&PID_0000\XYZ", vetoed);
        var result = service.EjectDrive("E:");
        Assert.False(result.Success);
        Assert.Equal("OutstandingOpen", result.VetoType);
        Assert.Equal("notepad.exe", result.VetoName);
    }

    [Fact]
    public void EjectResult_ToReportText_ContainsKeyFields()
    {
        var result = new EjectResult
        {
            Success = false,
            DriveLetter = "F:",
            AttemptTime = new DateTime(2025, 7, 1, 10, 0, 0),
            NativeReturnCode = 17,
            VetoType = "OutstandingOpen",
            VetoName = "explorer.exe",
            PnpDeviceId = @"USB\VID_1234&PID_5678\000",
            Message = "The device has open handles."
        };
        var text = result.ToReportText();
        Assert.Contains("F:", text);
        Assert.Contains("Failed", text);
        Assert.Contains("OutstandingOpen", text);
        Assert.Contains("explorer.exe", text);
        Assert.Contains("17", text);
    }

    [Fact]
    public void EjectResult_StaticHelpers_CreateCorrectObjects()
    {
        var failed = EjectResult.Failed("G:", "Test error", 42);
        Assert.False(failed.Success);
        Assert.Equal("G:", failed.DriveLetter);
        Assert.Equal("Test error", failed.Message);
        Assert.Equal(42, failed.NativeReturnCode);

        var ok = EjectResult.Ok("H:", @"USB\VID_0000\TEST");
        Assert.True(ok.Success);
        Assert.Equal("H:", ok.DriveLetter);
        Assert.Contains("safely removed", ok.Message);
    }
}
