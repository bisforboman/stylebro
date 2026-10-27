# Trial 23: Visual Basic projects are left alone (no whole run, '--exclude **/*.vb', a .vb a fix reached is put back), and a
# C# rename doesn't break the VB code that uses the name. A VB project references the C# library, which is formatted. (The
# namespace isn't 'Lib': a Visual Basic keyword.)
# The old engine (--engine dotnet-format) calls every fixer of an id, and in a run that loads a VB project the VB fixer of
# IDE0036 deleted a C# method (found here); the host now calls only fixers for the diagnostics' language, and the old engine
# leaves the C# projects whose load reaches VB out of its runs and names them (owner's decision 2026-10-27).
@{
    Description = 'a Visual Basic project next to C#'
    EnginesDiffer = 'the old engine leaves the tests (which reference VbApp) out of its runs, by decision'
    # check --build then finds what the left-out tests still have: Not clean (2), as it should.
    EngineExpect = @{ 'dotnet-format' = @{ format = 3; check = 3; 'check-build' = 2 } }
    EngineReasons = @{ 'dotnet-format' = @{ format = 'Lib\.Tests\.csproj: it references a Visual Basic project'; check = 'Lib\.Tests\.csproj: it references a Visual Basic project' } }
    Assert = {
        param($repo, $runs)
        if ((Get-Content -Raw (Join-Path $repo 'src/VbApp/Module1.vb')) -cne (Get-Content -Raw (Join-Path $repo '../origin/src/VbApp/Module1.vb'))) { 'the .vb file changed' }
        foreach ($engine in $runs.Keys) {
            if ((Get-Content -Raw (Join-Path $runs[$engine].Clone 'src/Lib/Calc.cs')) -notmatch 'int Twice\(') { "$engine deleted Calc.Twice" }
        }
    }
    Create = {
        param($root)
        New-Project $root 'src/Lib/Lib.csproj'
        Set-Messy $root 'src/Lib/Calc.cs' -Namespace 'CsLib'
        New-Project $root 'src/VbApp/VbApp.vbproj' -References '../Lib/Lib.csproj' -Properties ([ordered]@{ RootNamespace = 'VbApp' })
        Set-Text $root 'src/VbApp/Module1.vb' @'
Public Module Module1
    Public Function Four() As Integer
        Return CsLib.Calc.Twice(2)
    End Function
End Module
'@
        New-TestProject $root 'tests/Lib.Tests/Lib.Tests.csproj' -References '../../src/VbApp/VbApp.vbproj'
        Set-Text $root 'tests/Lib.Tests/VbTests.cs' @'
using Xunit;

namespace Lib.Tests;

public class VbTests
{
    [Fact]
    public void Four() { Assert.Equal(4, VbApp.Module1.Four()); }
}
'@
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'src/VbApp/VbApp.vbproj', 'tests/Lib.Tests/Lib.Tests.csproj'
    }
}
