# Trial 23: format applies xunit.analyzers' assertion fixes where the repository has them at warning or error (xunit 2.9
# brings xunit.analyzers 1.16, whose fixes have Fix All). The fixed tests still pass.
@{
    Description = 'xUnit assertion rules at warning, with Fix All support'
    Assert = {
        param($repo)
        $text = Get-Content -Raw (Join-Path $repo 'tests/Lib.Tests/FlagsTests.cs')
        if ($text -notmatch 'Assert\.True\(' -or $text -notmatch 'Assert\.Empty\(') { 'format didn''t apply xUnit2004/xUnit2013' }
    }
    Create = {
        param($root)
        Set-Text $root '.editorconfig' "root = true`n`n[*.cs]`ndotnet_diagnostic.xUnit2004.severity = warning`ndotnet_diagnostic.xUnit2013.severity = warning"
        New-Project $root 'src/Lib/Lib.csproj'
        Set-Text $root 'src/Lib/Flags.cs' "namespace Lib;`n`npublic static class Flags`n{`n    public static bool On => true;`n`n    public static int[] None => [];`n}"
        New-TestProject $root 'tests/Lib.Tests/Lib.Tests.csproj' -References '../../src/Lib/Lib.csproj'
        Set-Text $root 'tests/Lib.Tests/FlagsTests.cs' @'
using Xunit;

namespace Lib.Tests;

public class FlagsTests
{
    [Fact]
    public void On()
    {
        Assert.Equal(true, Lib.Flags.On);
        Assert.Equal(0, Lib.Flags.None.Length);
    }
}
'@
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'tests/Lib.Tests/Lib.Tests.csproj'
    }
}
