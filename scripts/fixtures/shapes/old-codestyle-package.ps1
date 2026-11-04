# Trial 67 (moq4, backlog 079): a project that references Microsoft.CodeAnalysis.CSharp.CodeStyle 3.8.0 (a 2020 package). 'dotnet format's
# analyzer pass ran that version's IDE0055 fix besides the SDK's: it knows neither C# 10's extended property patterns nor C# 14's
# `extension(...)`, wrote `Items.Count : 1`, and the SDK's own IDE0055 reported it again (after format: 47 findings that check
# then called "only this OS's line ending"). The analyzer pass now leaves the IDE ids to the style pass.
@{
    BuildFails = 'the package''s own IDE0055 reports the SDK''s form of the pattern (owner 2026-11-04: the SDK wins; check --build says Not clean)'
    Expect = @{ 'check-build' = 2 }
    Description = 'an old Microsoft.CodeAnalysis.CSharp.CodeStyle reference and an extended property pattern'
    Assert = {
        param($repo)
        $text = [IO.File]::ReadAllText((Join-Path $repo 'src/Lib/Boxes.cs'))
        if ($text -match 'Count\s+:\s+1') { 'the old package''s IDE0055 fix put a space before the colon of the property pattern' }
    }
    Create = {
        param($root)
        New-Project $root 'src/Lib/Lib.csproj' -Packages @{ 'Microsoft.CodeAnalysis.CSharp.CodeStyle' = '3.8.0' }
        Set-Text $root 'src/Lib/Boxes.cs' @'
using System.Collections.Generic;

namespace Lib;

public sealed record Box(List<int> Items);

public static class Boxes
{
    public static bool IsSingle(Box box) { return box is { Items.Count: 1 }; }
}
'@
        New-TestProject $root 'tests/Lib.Tests/Lib.Tests.csproj' -References '../../src/Lib/Lib.csproj'
        Set-Text $root 'tests/Lib.Tests/BoxesTests.cs' @'
using System.Collections.Generic;
using Xunit;

namespace Lib.Tests;

public class BoxesTests
{
    [Fact]
    public void IsSingle() { Assert.True(Lib.Boxes.IsSingle(new Lib.Box(new List<int> { 1 }))); }
}
'@
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'tests/Lib.Tests/Lib.Tests.csproj'
    }
}
