# Trial 62 (rebus-org/Rebus): a library for net6.0 and net10.0 with a package that doesn't support net6.0 (System.IO.Hashing
# 10.0.x: "doesn't support net6.0 and has not been tested with it", a build warning), and a net10.0-only test project. The
# host loads every framework at once, and MSBuild reported the library's net6.0 warning under the test project, which only
# the net10.0 run has: the host's load-warning note named the net10.0 run, the old engine's the net6.0 run.
@{
    Description = 'a package that warns about one of a multi-targeted project''s frameworks'
    Assert = {
        param($formatted, $runs, $context)
        $named = @{}
        foreach ($engine in $runs.Keys) {
            $text = $runs[$engine].Steps['format'].Text
            $named[$engine] = ([regex]::Matches($text, "had warnings loading the workspace in its (\S+) run") | ForEach-Object { $_.Groups[1].Value }) -join ','
            if ($named[$engine] -match 'net10\.0') { "$engine named the net10.0 run for the net6.0 warning:`n$text" }
        }
        if (@($named.Values | Select-Object -Unique).Count -gt 1) { "the engines' load-warning notes name different runs: $(($named.GetEnumerator() | ForEach-Object { "$($_.Key) '$($_.Value)'" }) -join ', ')" }
    }
    Create = {
        param($root)
        New-Project $root 'src/Lib/Lib.csproj' -Frameworks 'net6.0;net10.0' -Packages ([ordered]@{ 'System.IO.Hashing' = '10.0.6' }) -Properties ([ordered]@{ CheckEolTargetFramework = 'false' })
        Set-Messy $root 'src/Lib/Calc.cs' -Namespace 'Lib'
        Set-Text $root 'src/Lib/Hash.cs' "namespace Lib;`n`npublic static class Hash`n{`n    public static ulong Of(byte[] data) => System.IO.Hashing.XxHash64.HashToUInt64(data);`n}"
        New-TestProject $root 'tests/Lib.Tests/Lib.Tests.csproj' -References '../../src/Lib/Lib.csproj'
        Set-Text $root 'tests/Lib.Tests/CalcTests.cs' @'
using Xunit;

namespace Lib.Tests;

public class CalcTests
{
    [Fact]
    public void Twice() { Assert.Equal(4, Lib.Calc.Twice(2)); }

    [Fact]
    public void Hashes() { Assert.NotEqual(0UL, Lib.Hash.Of(new byte[] { 1 })); }
}
'@
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'tests/Lib.Tests/Lib.Tests.csproj'
    }
}
