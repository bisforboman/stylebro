# The plain shape every other one varies: a library and its xUnit tests in one solution, nothing unusual. If this fails,
# every other shape will too.
@{
    Description = 'a library and its tests, one solution'
    Create = {
        param($root)
        New-Project $root 'src/Lib/Lib.csproj'
        Set-Text $root 'src/Lib/Calc.cs' @'
namespace Lib;

public class Calc
{
    static public int Twice(int x) { return x * 2; }

    public string Name = "";
}
'@
        New-TestProject $root 'tests/Lib.Tests/Lib.Tests.csproj' -References '../../src/Lib/Lib.csproj'
        Set-Text $root 'tests/Lib.Tests/CalcTests.cs' @'
using Xunit;

namespace Lib.Tests;

public class CalcTests
{
    [Fact]
    public void Twice() { Assert.Equal(4, Lib.Calc.Twice(2)); }
}
'@
        New-Solution $root 'Basic.sln' 'src/Lib/Lib.csproj', 'tests/Lib.Tests/Lib.Tests.csproj'
    }
}
