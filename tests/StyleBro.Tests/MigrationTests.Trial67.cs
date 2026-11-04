using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the sixty-seventh trial (Dumpify, RestEase, CQRSlite, moq4 again) found in the tool.</summary>
public sealed partial class MigrationTests
{
    // RestEase: `#pragma warning disable IDE0040` around an implicit-private nested interface a test reads the source of. init turns
    // IDE0040 off for BRO1404, which ignored the pragma and added `private` (the diagnostic moved from column 19 to 27).
    [Fact]
    public void Init_CarriesIde0040SuppressionsOver_ToTheRulesThatReplaceIt()
    {
        Write("Directory.Build.props", "<Project><PropertyGroup><NoWarn>$(NoWarn);IDE0040</NoWarn></PropertyGroup></Project>");
        Write("A.cs", "class A\n{\n#pragma warning disable IDE0040 // Add accessibility modifiers\n    interface I\n#pragma warning restore IDE0040 // Add accessibility modifiers\n    {\n    }\n}\n");
        Write("B.cs", "[System.Diagnostics.CodeAnalysis.SuppressMessage(\"Style\", \"IDE0040:Add accessibility modifiers\", Justification = \"x\")]\nclass B { }\n");

        var preview = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root })));

        Assert.Contains("Suppressions: ", preview);
        Assert.Contains("to add", preview);
        Assert.DoesNotContain("BRO1404", File.ReadAllText(Path.Combine(root, "A.cs")));

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        var a = File.ReadAllText(Path.Combine(root, "A.cs"));
        Assert.Contains("#pragma warning disable IDE0040, BRO1007, BRO1404 // Add accessibility modifiers", a);
        Assert.Contains("#pragma warning restore IDE0040, BRO1007, BRO1404 // Add accessibility modifiers", a);
        Assert.Contains("\"BRO1404\"", File.ReadAllText(Path.Combine(root, "B.cs")));
        Assert.Contains("IDE0040;BRO1007;BRO1404", File.ReadAllText(Path.Combine(root, "Directory.Build.props")));

        // Running it again adds nothing.
        var again = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Equal(a, File.ReadAllText(Path.Combine(root, "A.cs")));
        Assert.DoesNotContain("Suppressions: ", again);
    }
}
