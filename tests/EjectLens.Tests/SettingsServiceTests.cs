using EjectLens.Models;
using EjectLens.Services;
using Xunit;

namespace EjectLens.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempDir;

    public SettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "EjectLensTests_" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void Load_ReturnsDefaultsWhenNoFile()
    {
        var service = new SettingsService(_tempDir);
        var settings = service.Load();

        Assert.NotNull(settings);
        Assert.Equal("system", settings.Language);
        Assert.Equal("medium", settings.DefaultWindowSize);
        Assert.True(settings.RememberWindowSize);
        Assert.True(settings.ConfirmBeforeEject);
    }

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        var service = new SettingsService(_tempDir);
        var original = new AppSettings
        {
            Language = "zh-CN",
            Theme = "dark",
            DefaultWindowSize = "small",
            ConfirmBeforeEject = false,
            DefaultTimeRange = "1hour"
        };

        service.Save(original);
        var loaded = service.Load();

        Assert.Equal("zh-CN", loaded.Language);
        Assert.Equal("dark", loaded.Theme);
        Assert.Equal("small", loaded.DefaultWindowSize);
        Assert.False(loaded.ConfirmBeforeEject);
        Assert.Equal("1hour", loaded.DefaultTimeRange);
    }

    [Fact]
    public void GetDefaultSize_ReturnsCorrectDimensions()
    {
        var small = new AppSettings { DefaultWindowSize = "small" };
        var medium = new AppSettings { DefaultWindowSize = "medium" };
        var large = new AppSettings { DefaultWindowSize = "large" };

        Assert.Equal(900, small.GetDefaultSize().Width);
        Assert.Equal(600, small.GetDefaultSize().Height);
        Assert.Equal(1100, medium.GetDefaultSize().Width);
        Assert.Equal(700, medium.GetDefaultSize().Height);
        Assert.Equal(1300, large.GetDefaultSize().Width);
        Assert.Equal(850, large.GetDefaultSize().Height);
    }

    [Fact]
    public void CreateDefaults_HasSensibleValues()
    {
        var defaults = AppSettings.CreateDefaults();
        Assert.Equal("system", defaults.Language);
        Assert.Equal("system", defaults.Theme);
        Assert.True(defaults.RememberWindowSize);
        Assert.False(defaults.StartMaximized);
    }

    [Fact]
    public void Load_CorruptJson_ReturnsDefaults()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "settings.json"), "this is not json {{{{");

        var service = new SettingsService(_tempDir);
        var settings = service.Load();

        Assert.NotNull(settings);
        Assert.Equal("system", settings.Language);
    }
}
