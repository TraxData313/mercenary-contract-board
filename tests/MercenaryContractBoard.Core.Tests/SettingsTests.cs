using MercenaryContractBoard.Core;

namespace MercenaryContractBoard.Core.Tests;

public class SettingsTests
{
    // tools\deploy.ps1 re-badges exactly this Id and Name in module\SubModule.xml; keep them in step.
    [Fact]
    public void ModuleIdMatchesManifest()
    {
        var manifest = File.ReadAllText(Path.Combine(RepoRoot(), "module", "SubModule.xml"));
        Assert.Contains("<Id value=\"" + ModInfo.ModuleId + "\" />", manifest);
        Assert.Contains("<Name value=\"" + ModInfo.DisplayName + "\" />", manifest);
        Assert.Contains("<DLLName value=\"MercenaryContractBoard.dll\" />", manifest);
    }

    [Fact]
    public void EverySettingIsAPropertyOfBoardSettings()
    {
        var names = typeof(BoardSettings).GetProperties().Select(p => p.Name).ToHashSet();
        Assert.All(SettingsMenu.All, s => Assert.Contains(s.Id, names));
        Assert.Equal(SettingsMenu.All.Count, SettingsMenu.All.Select(s => s.Id).Distinct().Count());
        Assert.Equal(names.Count, SettingsMenu.All.Count);
    }

    [Fact]
    public void DefaultsAreAllOn()
    {
        var s = new BoardSettings();
        Assert.All(SettingsMenu.All, m => Assert.Equal(1, m.Get(s)));
    }

    [Fact]
    public void SetWritesAndReportsAChange()
    {
        var s = new BoardSettings();
        var m = SettingsMenu.Find(nameof(BoardSettings.ShowUnavailable))!;
        Assert.True(m.Set(s, 0));
        Assert.False(s.ShowUnavailable);
        Assert.False(m.Set(s, 0));
        Assert.False(m.Set(s, double.NaN));
    }

    [Fact]
    public void EveryGroupHasSettingsAndEverySettingAHint()
    {
        Assert.All(SettingsMenu.Groups, g => Assert.NotEmpty(SettingsMenu.InGroup(g)));
        Assert.All(SettingsMenu.All, m => Assert.False(string.IsNullOrWhiteSpace(m.Hint)));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "MercenaryContractBoard.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("MercenaryContractBoard.sln not found above " + AppContext.BaseDirectory);
    }
}
