# Trial 67 (RestEase): `#pragma warning disable IDE0040` around an interface that is private on purpose (a test reads the source
# and expects the diagnostic at the `interface` keyword's column). init turns IDE0040 off for BRO1404/BRO1007, which ignored the
# pragma and added `private`. Owner's choice (2026-11-04): init carries IDE0040 suppressions over, like migrate does for SA ids.
@{
    Description = 'a #pragma warning disable IDE0040 around an implicitly private nested interface'
    InitReason = 'Suppressions: .* for the rules that replace the built-in rules'
    Assert = {
        param($repo)
        $text = [IO.File]::ReadAllText((Join-Path $repo 'src/Lib/Outer.cs'))
        if ($text -match 'private\s+interface\s+IImplicit') { 'BRO1404 added private inside the IDE0040 pragma pair' }
        if ($text -notmatch 'disable IDE0040, BRO1007, BRO1404') { 'init did not add BRO1007 and BRO1404 to the IDE0040 pragma' }
    }
    Create = {
        param($root)
        New-Project $root 'src/Lib/Lib.csproj'
        Set-Text $root 'src/Lib/Outer.cs' @'
namespace Lib;

public class Outer
{
#pragma warning disable IDE0040 // Add accessibility modifiers
    interface IImplicit
#pragma warning restore IDE0040 // Add accessibility modifiers
    {
        int Value();
    }

    int Use(IImplicit item) { return item.Value(); }
}
'@
        New-TestProject $root 'tests/Lib.Tests/Lib.Tests.csproj' -References '../../src/Lib/Lib.csproj'
        Set-Text $root 'tests/Lib.Tests/OuterTests.cs' @'
using Xunit;

namespace Lib.Tests;

public class OuterTests
{
    [Fact]
    public void Constructs() { Assert.NotNull(new Lib.Outer()); }
}
'@
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'tests/Lib.Tests/Lib.Tests.csproj'
    }
}
