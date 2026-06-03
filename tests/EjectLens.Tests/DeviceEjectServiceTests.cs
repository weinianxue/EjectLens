using EjectLens.Models;
using EjectLens.Services;
using Xunit;

namespace EjectLens.Tests;

public class DeviceEjectServiceTests
{
    private sealed class TestDeviceEjectService : DeviceEjectService
    {
        private readonly string? _pnpDeviceId;
        private readonly List<EjectCandidate> _candidates;
        private readonly string? _fallbackResult;

        public TestDeviceEjectService(
            string? pnpDeviceId,
            List<EjectCandidate> candidates,
            string? fallbackResult)
        {
            _pnpDeviceId = pnpDeviceId;
            _candidates = candidates;
            _fallbackResult = fallbackResult;
        }

        public override string? ResolvePnpDeviceId(string driveLetter)
            => _pnpDeviceId;

        public override int LocateDiskDevNode(string diskPnpId, out int diskDevInst)
        {
            diskDevInst = 1;
            return EjectLens.Native.DeviceEjectNative.CR_SUCCESS;
        }

        public override List<EjectCandidate> BuildCandidateList(
            int diskDevInst, string diskPnpId, string driveLetter)
            => _candidates;

        public override void TryEjectCandidate(EjectCandidate candidate)
        {
            candidate.WasTried = true;
        }

        public override string? TrySafeVolumeEject(string driveLetter)
            => _fallbackResult;
    }

    [Fact]
    public void EjectDrive_NoDriveSelected_ReturnsFailed()
    {
        var service = new TestDeviceEjectService(null, [], null);
        var result = service.EjectDrive("");
        Assert.False(result.Success);
        Assert.Contains("No drive selected", result.Message);
    }

    [Fact]
    public void EjectDrive_CannotResolvePnpDevice_ReturnsFailed()
    {
        var service = new TestDeviceEjectService(null, [], null);
        var result = service.EjectDrive("D:");
        Assert.False(result.Success);
        Assert.Contains("Could not resolve", result.Message);
    }

    [Fact]
    public void EjectDrive_SuccessfulCandidate_ReturnsOk()
    {
        var okCandidate = new EjectCandidate
        {
            DeviceInstanceId = @"USB\VID_0781&PID_5591\ABC123",
            Level = "Parent2",
            IsLikelyRemovableTarget = true,
            ReturnCode = 0,
        };

        var service = new TestDeviceEjectService(
            @"USBSTOR\DISK&VEN_USB\XYZ",
            [okCandidate],
            null);

        var result = service.EjectDrive("D:");

        Assert.True(result.Success);
        Assert.Equal("D:", result.DriveLetter);
        Assert.Contains("safely removed", result.Message);
        Assert.NotNull(result.SuccessfulCandidate);
        Assert.Equal("Parent2", result.SuccessfulCandidate!.Level);
    }

    [Fact]
    public void EjectDrive_DiskNodeVetoed_ParentSucceeds()
    {
        var diskNode = new EjectCandidate
        {
            DeviceInstanceId = @"USBSTOR\DISK&VEN_USB&PROD_SANDISK\000000",
            Level = "DiskDrive",
            IsLikelyRemovableTarget = false,
            ReturnCode = 0x11,
            VetoType = "Driver",
            VetoName = "USBSTOR",
        };

        var usbParent = new EjectCandidate
        {
            DeviceInstanceId = @"USB\VID_0781&PID_5591\ABC123",
            Level = "Parent2",
            IsLikelyRemovableTarget = true,
            ReturnCode = 0,
        };

        var service = new TestDeviceEjectService(
            @"USBSTOR\DISK&VEN_USB\000",
            [usbParent, diskNode],
            null);

        var result = service.EjectDrive("J:");

        Assert.True(result.Success);
        Assert.Equal("Parent2", result.SuccessfulCandidate!.Level);
    }

    [Fact]
    public void EjectDrive_AllCandidatesFail_FallbackReported()
    {
        var candidates = new List<EjectCandidate>
        {
            new() {
                DeviceInstanceId = @"USB\VID_0781&PID_5591",
                Level = "Parent2", IsLikelyRemovableTarget = true,
                ReturnCode = 0x11, VetoType = "Driver",
            },
            new() {
                DeviceInstanceId = @"USBSTOR\DISK&VEN_USB",
                Level = "Parent1", IsLikelyRemovableTarget = true,
                ReturnCode = 0x11, VetoType = "Driver",
            },
            new() {
                DeviceInstanceId = @"USBSTOR\DISK&VEN_USB\000",
                Level = "DiskDrive",
                ReturnCode = 0x11, VetoType = "Driver",
            },
        };

        var service = new TestDeviceEjectService(
            @"USBSTOR\DISK&VEN_USB\000",
            candidates,
            "Volume lock failed. Error: 32.");

        var result = service.EjectDrive("J:");

        Assert.False(result.Success);
        Assert.Equal(3, result.AttemptedCandidates.Count);
        Assert.All(result.AttemptedCandidates, c => Assert.True(c.WasTried));
        Assert.True(result.FallbackAttempted);
        Assert.Contains("lock failed", result.FallbackResult);
        Assert.NotEmpty(result.SuggestedNextSteps);
    }

    [Fact]
    public void EjectDrive_WithSanDiskDevice_DriverVetoMappedCorrectly()
    {
        var candidates = new List<EjectCandidate>
        {
            new() {
                DeviceInstanceId = @"USB\VID_0781&PID_5591\1234567890",
                Level = "Parent2", IsLikelyRemovableTarget = true,
                ReturnCode = 0x11, VetoType = "Driver",
            },
            new() {
                DeviceInstanceId = @"USBSTOR\DISK&VEN_USB&PROD_SANDISK_3.2GEN1\ABC&0",
                Level = "Parent1", IsLikelyRemovableTarget = true,
                ReturnCode = 0x11, VetoType = "Driver",
            },
            new() {
                DeviceInstanceId = @"USBSTOR\DISK&VEN_USB&PROD_SANDISK_3.2GEN1\000000",
                Level = "DiskDrive", IsLikelyRemovableTarget = false,
                ReturnCode = 0x11, VetoType = "Driver",
            }
        };

        var service = new TestDeviceEjectService(
            @"USBSTOR\DISK&VEN_USB&PROD_SANDISK_3.2GEN1\000000",
            candidates,
            "Volume lock failed. Error: 32.");

        var result = service.EjectDrive("J:");

        Assert.False(result.Success);
        Assert.Equal("CR_REMOVE_VETOED", result.ReturnCodeName);
        Assert.Equal("Driver", result.VetoType);
        Assert.Equal(3, result.AttemptedCandidates.Count);
        Assert.All(result.AttemptedCandidates, c => Assert.True(c.WasTried));

        Assert.StartsWith("USB\\", result.AttemptedCandidates[0].DeviceInstanceId);
        Assert.True(result.AttemptedCandidates[0].IsLikelyRemovableTarget);

        var report = result.ToReportText();
        Assert.Contains("CR_REMOVE_VETOED", report);
        Assert.Contains("Driver", report);
        Assert.Contains("Attempted device nodes", report);
        Assert.Contains("USB\\", report);
    }

    [Fact]
    public void EjectResult_Failed_WithVetoCode_MapsName()
    {
        var failed = EjectResult.Failed("G:", "Test error", 17);
        Assert.False(failed.Success);
        Assert.Equal("G:", failed.DriveLetter);
        Assert.Equal(17, failed.ReturnCode);
        Assert.Equal("CR_REMOVE_VETOED", failed.ReturnCodeName);
        Assert.NotEmpty(failed.SuggestedNextSteps);
    }

    [Fact]
    public void ReturnCodes_MapSymbolically()
    {
        Assert.Equal("CR_SUCCESS",
            EjectLens.Native.DeviceEjectNative.GetReturnCodeName(0x00));
        Assert.Equal("CR_REMOVE_VETOED",
            EjectLens.Native.DeviceEjectNative.GetReturnCodeName(0x11));
        Assert.Equal("CR_ACCESS_DENIED",
            EjectLens.Native.DeviceEjectNative.GetReturnCodeName(0x2A));
        Assert.Equal("UNKNOWN (0xFF)",
            EjectLens.Native.DeviceEjectNative.GetReturnCodeName(0xFF));
    }

    [Fact]
    public void CandidateSorting_PrioritizesUSBParents()
    {
        var candidates = new List<EjectCandidate>
        {
            new() { DeviceInstanceId = @"USBSTOR\DISK\000", Level = "DiskDrive",
                     IsLikelyRemovableTarget = false },
            new() { DeviceInstanceId = @"USB\VID_0000\001", Level = "Parent2",
                     IsLikelyRemovableTarget = true },
            new() { DeviceInstanceId = @"USBSTOR\DISK", Level = "Parent1",
                     IsLikelyRemovableTarget = true },
            new() { DeviceInstanceId = @"PCI\VEN_8086", Level = "Parent3",
                     IsLikelyRemovableTarget = false },
        };

        candidates.Sort((a, b) =>
        {
            int Score(EjectCandidate c)
            {
                if (c.Level.StartsWith("Parent") && c.IsLikelyRemovableTarget) return 100;
                if (c.Level.StartsWith("Parent")) return 60;
                if (c.Level == "DiskDrive") return 10;
                return 5;
            }
            return Score(b).CompareTo(Score(a));
        });

        Assert.StartsWith("USB\\", candidates[0].DeviceInstanceId);
        Assert.Equal("DiskDrive", candidates[^1].Level);
    }
}
