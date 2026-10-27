# Trials 23 and 44: xUnit assertion rules. format applies xunit.analyzers' fixes where they are warnings or errors; versions
# 1.24 and later have no Fix All for them, so format leaves them and says so (XunitGap, a note like IDE0049's), both on a
# fresh clone and after a restore. One test project with xunit 2.9 (its analyzers have Fix All), one with xunit.analyzers
# 1.24 or later.
@{
    Description = 'xUnit with and without Fix All support'
    Before = {
        param($repo)
        dotnet restore $repo --nologo -v q | Out-Null
    }
    Create = {
        param($root)
        Set-Text $root '.editorconfig' "root = true`n`n[*.cs]`ndotnet_diagnostic.xUnit2004.severity = warning`ndotnet_diagnostic.xUnit2013.severity = warning"
        New-Project $root 'src/Lib/Lib.csproj'
        Set-Text $root 'src/Lib/Flags.cs' "namespace Lib;`n`npublic static class Flags`n{`n    public static bool On => true;`n`n    public static int[] None => [];`n}"
        foreach ($name in 'Old', 'New') {
            $packages = if ($name -eq 'New') { [ordered]@{ 'xunit.analyzers' = '1.25.0' } } else { [ordered]@{} }
            New-TestProject $root "tests/$name.Tests/$name.Tests.csproj" -References '../../src/Lib/Lib.csproj' -Packages $packages
            Set-Text $root "tests/$name.Tests/FlagsTests.cs" @"
using Xunit;

namespace $name.Tests;

public class FlagsTests
{
    [Fact]
    public void On()
    {
        Assert.Equal(true, Lib.Flags.On);
        Assert.Equal(0, Lib.Flags.None.Length);
    }
}
"@
        }
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'tests/Old.Tests/Old.Tests.csproj', 'tests/New.Tests/New.Tests.csproj'
    }
}
