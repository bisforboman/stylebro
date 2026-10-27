# Trial 44 (charls): xunit.analyzers 1.24 and later have no Fix All for their fixes, so format can't apply or check them.
# Owner's choice: a note like IDE0049's (XunitGap), printed by the preview, format and check alike, on a fresh clone and after
# a restore; the verdict doesn't count it. The findings stay for the build (the repository's own TWAE build fails on them,
# before and after StyleBro). A repository mixing versions gets the note for all of its test projects (one run, one id list).
@{
    Description = 'xunit.analyzers 1.25 (no Fix All): the note, before and after a restore'
    Reasons = @{ preview = 'no Fix All'; format = 'no Fix All'; check = 'no Fix All' }
    BuildFails = 'xUnit2004 at warning, which format can''t fix with xunit.analyzers 1.25 (the note says so)'
    Before = {
        param($repo)
        if ((Split-Path $repo -Leaf) -eq 'host') { dotnet restore $repo --nologo -v q | Out-Null }
    }
    Create = {
        param($root)
        Set-Text $root '.editorconfig' "root = true`n`n[*.cs]`ndotnet_diagnostic.xUnit2004.severity = warning"
        New-Project $root 'src/Lib/Lib.csproj'
        Set-Messy $root 'src/Lib/Calc.cs' -Namespace 'Lib'
        New-TestProject $root 'tests/Lib.Tests/Lib.Tests.csproj' -References '../../src/Lib/Lib.csproj' -Packages ([ordered]@{ 'xunit.analyzers' = '1.25.0' })
        Set-Text $root 'tests/Lib.Tests/CalcTests.cs' @'
using Xunit;

namespace Lib.Tests;

public class CalcTests
{
    [Fact]
    public void Twice() { Assert.Equal(true, Lib.Calc.Twice(2) == 4); }
}
'@
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'tests/Lib.Tests/Lib.Tests.csproj'
    }
}
