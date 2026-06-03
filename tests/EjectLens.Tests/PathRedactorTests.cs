using EjectLens.Utils;
using Xunit;

namespace EjectLens.Tests;

public class PathRedactorTests
{
    [Fact]
    public void Redact_ReplacesUserProfilePath()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrEmpty(userProfile))
            return;

        var input = $"Path is {userProfile}\\Documents\\file.txt";
        var result = PathRedactor.Redact(input);

        Assert.DoesNotContain(userProfile, result);
        Assert.Contains("%USERPROFILE%", result);
        Assert.Contains("%USERPROFILE%\\Documents\\file.txt", result);
    }

    [Fact]
    public void Redact_HandlesEmptyString()
    {
        var result = PathRedactor.Redact("");
        Assert.Equal("", result);

        result = PathRedactor.Redact(null!);
        Assert.Null(result);
    }

    [Fact]
    public void Redact_PreservesNonProfilePaths()
    {
        var input = "C:\\Windows\\System32\\notepad.exe";
        var result = PathRedactor.Redact(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void Redact_CaseInsensitiveReplacement()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrEmpty(userProfile))
            return;

        var mixedCase = userProfile.ToUpperInvariant();
        if (mixedCase == userProfile)
            mixedCase = userProfile.ToLowerInvariant();

        var input = $"Path is {mixedCase}\\test.txt";
        var result = PathRedactor.Redact(input);

        Assert.Contains("%USERPROFILE%", result);
        Assert.DoesNotContain(mixedCase, result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_MultipleOccurrences()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrEmpty(userProfile))
            return;

        var input = $"A: {userProfile}\\a.txt B: {userProfile}\\b.txt";
        var result = PathRedactor.Redact(input);

        var occurrences = result.Split("%USERPROFILE%").Length - 1;
        Assert.Equal(2, occurrences);
    }

    [Fact]
    public void RedactFull_ReplacesAppDataPaths()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        if (string.IsNullOrEmpty(appData))
            return;

        var input = $"Config in {appData}\\MyApp\\config.json";
        var result = PathRedactor.RedactFull(input);

        Assert.DoesNotContain(appData, result);
        Assert.Contains("%APPDATA%", result);
    }
}
