# Trial 63 (mono/t4): a test project for net6.0 and net7.0 on a fresh clone. Loading the projects evaluated a
# multi-targeted project's first framework before the restore, and the run plan reused it: without the restore,
# Microsoft.NET.Test.Sdk's Program.cs (a Compile item its restored build props add) was missing from that framework's
# files only, so the two frameworks looked different and the first format or check of every clone made a run the later
# ones merge ("net7.0: no run of its own"). t4 sets LangVersion, as this does: without it each framework has its own C#
# version and they never merge.
@{
    Description = 'a multi-targeted test project on a fresh clone (no restore output yet)'
    Assert = {
        param($formatted, $runs, $context)
        foreach ($engine in $runs.Keys) {
            $text = $runs[$engine].Steps['format'].Text
            if ($text -notmatch 'net8\.0: no run of its own') { "$engine's format made a net8.0 run of its own:`n$text" }
        }
    }
    Create = {
        param($root)
        New-Project $root 'src/Lib/Lib.csproj' -Frameworks 'net8.0;net10.0' -Properties ([ordered]@{ LangVersion = 'latest' })
        Set-Messy $root 'src/Lib/Calc.cs' -Namespace 'Lib'
        New-TestProject $root 'tests/Lib.Tests/Lib.Tests.csproj' -Frameworks 'net8.0;net10.0' -Properties ([ordered]@{ LangVersion = 'latest' }) -References '../../src/Lib/Lib.csproj'
        Set-Text $root 'tests/Lib.Tests/CalcTests.cs' "using Xunit;`n`nnamespace Lib.Tests;`n`npublic class CalcTests`n{`n    [Fact]`n    public void Twice() { Assert.Equal(4, Lib.Calc.Twice(2)); }`n}`n"
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'tests/Lib.Tests/Lib.Tests.csproj'
    }
}
