using StyleBro.Migrate;

namespace StyleBro.Tests;

public sealed partial class MigrationTests
{
    [Fact]
    public void Init_SetsStyleBroRulesById_WhereAGlobalConfigsBulkSeverityHidesThemFromFormat()
    {
        // Trial 43, NServiceBus.Gateway: src/.globalconfig turned every analyzer off, src/.editorconfig said 'default' for C#;
        // 'dotnet format' skipped StyleBro's rules (check said clean), the build reported them (the preset's entries win).
        Write(".editorconfig", "root = true\n\n[*.cs]\nindent_size = 4\n");
        Write("src/.globalconfig", "is_global = true\n\ndotnet_analyzer_diagnostic.severity = none\n");
        Write("src/.editorconfig", "[*.cs]\ndotnet_analyzer_diagnostic.severity = default\n");
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/App/C.cs", "class C\n{\n}\n");
        Assert.StartsWith("Not clean: A .globalconfig's bulk severity", FormatCommand.HiddenLine(root), StringComparison.Ordinal);

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        Assert.Contains("dotnet_diagnostic.BRO1001.severity = warning\n", File.ReadAllText(Path.Combine(root, ".editorconfig")).Replace("\r\n", "\n"), StringComparison.Ordinal);
        Assert.Null(FormatCommand.HiddenLine(root));
    }

    [Fact]
    public void WithGlobalBulk_TakesTheLowestGlobalSeverity_UnderTheEditorConfigsOwn()
    {
        Write("a/.globalconfig", "is_global = true\ndotnet_analyzer_diagnostic.severity = suggestion\ndotnet_diagnostic.BRO1001.severity = none\n");
        Write("b/.globalconfig", "is_global = true\ndotnet_analyzer_diagnostic.severity = none\n");
        Write("c/.globalconfig", "dotnet_analyzer_diagnostic.category-Naming.severity = none\n");

        var merged = Migration.WithGlobalBulk(root, new Dictionary<string, string>());
        Assert.Equal("none", merged["dotnet_analyzer_diagnostic.severity"]);
        Assert.False(merged.ContainsKey("dotnet_diagnostic.BRO1001.severity"));
        Assert.False(merged.ContainsKey("dotnet_analyzer_diagnostic.category-Naming.severity"));

        // 'default' doesn't override a global bulk severity in 'dotnet format'; any other value does.
        Assert.Equal("none", Migration.WithGlobalBulk(root, new Dictionary<string, string> { ["dotnet_analyzer_diagnostic.severity"] = "default" })["dotnet_analyzer_diagnostic.severity"]);
        Assert.Equal("warning", Migration.WithGlobalBulk(root, new Dictionary<string, string> { ["dotnet_analyzer_diagnostic.severity"] = "warning" })["dotnet_analyzer_diagnostic.severity"]);
        Assert.Equal("default", Migration.WithGlobalBulk(Path.Combine(root, "c"), new Dictionary<string, string> { ["dotnet_analyzer_diagnostic.severity"] = "default" })["dotnet_analyzer_diagnostic.severity"]);
    }

    [Fact]
    public void Init_SaysWhenGitIgnoresTheEditorConfigItWrites()
    {
        // Trial 43, NSwag: '.editorconfig' in .gitignore, so init's settings would never be committed.
        Assert.Null(InitCommand.IgnoredLine(Path.Combine(root, ".editorconfig"), ".editorconfig"));
        PreviewCommand.Git(root, "init", "-q");
        Write(".gitignore", "bin/\n.editorconfig\n");
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("App/C.cs", "class C\n{\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Contains("Note: git ignores .editorconfig (.gitignore line 2: '.editorconfig'): commit it with 'git add -f .editorconfig'", output, StringComparison.Ordinal);

        Write(".gitignore", "bin/\n");
        Assert.Null(InitCommand.IgnoredLine(Path.Combine(root, ".editorconfig"), ".editorconfig"));
    }

    [Fact]
    public void AddIgnoredSettings_AddsTheIgnoredEditorConfigs_NotBuildOutput()
    {
        PreviewCommand.Git(root, "init", "-q");
        Write(".gitignore", ".editorconfig\n");
        Write(".editorconfig", "root = true\n");
        Write("src/.editorconfig", "[*.cs]\n");
        Write("src/App/obj/.editorconfig", "[*.cs]\n");
        Write("src/App/C.cs", "class C\n{\n}\n");

        PreviewCommand.AddIgnoredSettings(root);

        var staged = PreviewCommand.Git(root, "diff", "--cached", "--name-only").Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal([".editorconfig", "src/.editorconfig"], staged.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void HiddenLine_CountsAGlobalConfigAboveTheFolder_WithoutScanningAboveIt()
    {
        // The SDK gives a project the .globalconfig files of the folders above it. Only those files count from above: a scan
        // of everything below the outermost folder reached '/' without a 'root = true' .editorconfig (the test host spun).
        Write(".globalconfig", "is_global = true\ndotnet_analyzer_diagnostic.severity = silent\n");
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("Other/.globalconfig", "is_global = true\ndotnet_analyzer_diagnostic.severity = none\n");

        Assert.StartsWith("Not clean: A .globalconfig's bulk severity", FormatCommand.HiddenLine(Path.Combine(root, "App")), StringComparison.Ordinal);
        Assert.Equal("silent", Migration.WithGlobalBulk(Path.Combine(root, "App"), new Dictionary<string, string>(), [root])["dotnet_analyzer_diagnostic.severity"]);
    }
}
