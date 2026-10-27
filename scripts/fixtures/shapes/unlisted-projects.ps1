# Trials 29 and 30 (Moq): a project the tests reference isn't in the solution; format gives it to one run (OutsideProjectRuns)
# so the build that compiles it finds nothing left. A second test project no solution lists at all: nothing builds it, so
# it is left alone.
@{
    Description = 'a referenced project the solution doesn''t list, a test project no solution lists'
    Assert = {
        param($repo)
        if ((Get-Content -Raw (Join-Path $repo 'tests/Harness/Probe.cs')) -match 'static public') { 'the referenced project the solution doesn''t list was not formatted' }
    }
    Create = {
        param($root)
        New-Project $root 'src/Lib/Lib.csproj'
        Set-Messy $root 'src/Lib/Calc.cs' -Namespace 'Lib'
        New-Project $root 'tests/Harness/Harness.csproj' -References '../../src/Lib/Lib.csproj'
        Set-Messy $root 'tests/Harness/Probe.cs' -Namespace 'Harness'
        New-TestProject $root 'tests/Lib.Tests/Lib.Tests.csproj' -References '../../src/Lib/Lib.csproj', '../Harness/Harness.csproj'
        Set-Text $root 'tests/Lib.Tests/CalcTests.cs' @'
using Xunit;

namespace Lib.Tests;

public class CalcTests
{
    [Fact]
    public void Twice() { Assert.Equal(4, Harness.Probe.Twice(2)); }
}
'@
        New-TestProject $root 'tests/Other.Tests/Other.Tests.csproj' -References '../../src/Lib/Lib.csproj'
        Set-Text $root 'tests/Other.Tests/OtherTests.cs' @'
using Xunit;

namespace Other.Tests;

public class OtherTests
{
    [Fact]
    public void Twice() { Assert.Equal(4, Lib.Calc.Twice(2)); }
}
'@
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'tests/Lib.Tests/Lib.Tests.csproj'
    }
}
