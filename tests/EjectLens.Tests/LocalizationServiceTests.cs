using EjectLens.Services;
using Xunit;

namespace EjectLens.Tests;

public class LocalizationServiceTests
{
    [Fact]
    public void Get_ReturnsEnglishByDefault()
    {
        var loc = new LocalizationService();
        loc.SetLanguage("en-US");
        Assert.Equal("Drive:", loc.Get("Drive"));
        Assert.Equal("Settings", loc.Get("Settings"));
    }

    [Fact]
    public void Get_ReturnsChineseWhenSet()
    {
        var loc = new LocalizationService();
        loc.SetLanguage("zh-CN");
        Assert.Equal("驱动器:", loc.Get("Drive"));
        Assert.Equal("设置", loc.Get("Settings"));
    }

    [Fact]
    public void Get_FallsBackToEnglishForUnknownKey()
    {
        var loc = new LocalizationService();
        loc.SetLanguage("zh-CN");
        var result = loc.Get("NonExistentKey");
        Assert.Equal("NonExistentKey", result);
    }

    [Fact]
    public void SetLanguage_UnknownLanguageFallsBackToEnglish()
    {
        var loc = new LocalizationService();
        loc.SetLanguage("fr-FR");
        Assert.Equal("en-US", loc.CurrentLanguage);
        Assert.Equal("Drive:", loc.Get("Drive"));
    }

    [Fact]
    public void AllExpectedKeysExistInBothLocales()
    {
        var identityKeys = new HashSet<string>
        {
            "Refresh", "Settings", "Status", "Process", "PID", "Path", "State"
        };

        var keys = new[]
        {
            "Drive", "Refresh", "TimeRange", "ScanEvents",
            "CopyReport", "SaveReport", "OpenTaskManager",
            "ScanSelectedPath", "Settings", "EjectSelectedDrive",
            "EventDetails", "Status", "Process", "PID",
            "Path", "CommandLine", "AffectedDevice", "State",
            "EjectConfirmTitle", "EjectConfirmMessage",
            "EjectSuccess", "EjectFailed", "EjectVetoed",
            "EjectNotResolved", "EjectNeedAdmin",
            "SettingsTitle", "TabGeneral", "TabAppearance", "TabBehavior",
            "SettingsLanguage", "LangSystemDefault", "LangEnglish", "LangChinese",
            "SettingsTheme", "ThemeSystem", "ThemeLight", "ThemeDark",
            "SettingsDefaultSize", "SizeSmall", "SizeMedium", "SizeLarge",
            "SettingsRememberSize", "SettingsStartMaximized",
            "SettingsConfirmEject", "SettingsRefreshAfterEject",
            "SettingsRememberDrive", "SettingsDefaultTimeRange",
            "Time15min", "Time1hour", "Time2hours", "Time24hours"
        };

        var loc = new LocalizationService();

        loc.SetLanguage("en-US");
        foreach (var key in keys)
        {
            var val = loc.Get(key);
            Assert.NotNull(val);
            Assert.NotEmpty(val);
        }

        loc.SetLanguage("zh-CN");
        foreach (var key in keys)
        {
            var val = loc.Get(key);
            Assert.NotNull(val);
            Assert.NotEmpty(val);
            if (!identityKeys.Contains(key))
                Assert.NotEqual(key, val);
        }
    }

    [Fact]
    public void SystemLanguage_ChoosesBasedOnOS()
    {
        var loc = new LocalizationService();
        loc.SetLanguage("system");
        Assert.Contains(loc.CurrentLanguage, new[] { "en-US", "zh-CN" });
    }
}
