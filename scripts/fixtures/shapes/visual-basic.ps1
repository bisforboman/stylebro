# Trial 23: Visual Basic projects are left alone (no whole run, '--exclude **/*.vb', a .vb a fix reached is put back), and a
# C# rename doesn't break the VB code that uses the name. A VB project references the C# library, which is formatted.
@{
    Description = 'a Visual Basic project next to C#'
    Assert = {
        param($repo)
        if ((Get-Content -Raw (Join-Path $repo 'src/VbApp/Module1.vb')) -cne (Get-Content -Raw (Join-Path $repo '../origin/src/VbApp/Module1.vb'))) { 'the .vb file changed' }
    }
    Create = {
        param($root)
        New-Project $root 'src/Lib/Lib.csproj'
        Set-Messy $root 'src/Lib/Calc.cs' -Namespace 'Lib'
        New-Project $root 'src/VbApp/VbApp.vbproj' -References '../Lib/Lib.csproj' -Properties ([ordered]@{ RootNamespace = 'VbApp' })
        Set-Text $root 'src/VbApp/Module1.vb' @'
Public Module Module1
    Public Function Four() As Integer
        Return Lib.Calc.Twice(2)
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
