using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What trial 60 found in bro.</summary>
public sealed partial class MigrationTests
{
    // khellang/Middleware cloned without --recursive: ProblemDetails compiles 'lib/AspNetCore/src/Shared/**/*.cs' from a
    // submodule whose folder is empty, so it didn't compile, and format and check said clean (owner's decision 2026-10-29).
    [Fact]
    public void Format_NamesSubmodulesThatArentCheckedOut()
    {
        Write(".gitmodules", "[submodule \"lib/AspNetCore\"]\n\tpath = lib/AspNetCore\n\turl = https://github.com/dotnet/aspnetcore.git\n"
            + "[submodule \"lib/Gone\"]\n\tpath = lib\\Gone\n\turl = x\n[submodule \"lib/Here\"]\n\tpath = lib/Here\n\turl = y\n");
        Directory.CreateDirectory(Path.Combine(root, "lib", "AspNetCore"));
        Write("lib/Here/.git", "gitdir: ../../.git/modules/lib/Here\n");
        Write("lib/Here/Code.cs", "class Code { }\n");

        var missing = FormatCommand.UncheckedOutSubmodules(root);

        Assert.Equal(["lib/AspNetCore", "lib/Gone"], missing);
        var line = FormatCommand.MissingSubmoduleLine(missing);
        Assert.StartsWith("Missing: the git submodules lib/AspNetCore, lib/Gone aren't checked out:", line, StringComparison.Ordinal);
        Assert.Contains("IDE0005 (unused usings) is left out", line, StringComparison.Ordinal);
        Assert.Contains("git submodule update --init", line, StringComparison.Ordinal);
        Assert.Empty(FormatCommand.UncheckedOutSubmodules(Path.Combine(root, "lib")));
    }
}
