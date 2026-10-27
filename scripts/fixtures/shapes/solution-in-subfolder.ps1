# Trials 55 and 56 (Newtonsoft.Json.Schema, DynamicData): the only solution is in Src/. The preview's patch once had
# Src/stylebro.baseline where 'bro format' on the root wrote stylebro.baseline. A finding the fixes keep on purpose (a field
# read by reflection) makes format write a baseline; preview and format must agree on where.
@{
    Description = 'the only solution one folder down, a kept finding baselined'
    Target = 'Src/Fixture.sln'
    Assert = {
        param($repo)
        if (-not @(Get-ChildItem $repo -Recurse -Filter 'stylebro.baseline' | Where-Object FullName -notmatch '[\\/](bin|obj)[\\/]')) { 'format wrote no stylebro.baseline for the kept finding' }
    }
    Create = {
        param($root)
        New-Project $root 'Src/Lib/Lib.csproj' -Properties ([ordered]@{ Nullable = 'enable' })
        Set-Messy $root 'Src/Lib/Calc.cs' -Namespace 'Lib'
        Set-Text $root 'Src/Lib/Counter.cs' "namespace Lib;`n`npublic class Counter`n{`n    private int _count;`n    private int _total;`n`n    public int Next() => ++_count + _total++;`n}"
        Set-Text $root 'Src/Lib/Reader.cs' "namespace Lib;`n`npublic static class Reader`n{`n    public static object? Read(Counter counter) => typeof(Counter).GetField(`"_count`", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(counter);`n}"
        New-TestProject $root 'Src/Lib.Tests/Lib.Tests.csproj' -References '../Lib/Lib.csproj'
        Set-Text $root 'Src/Lib.Tests/ReaderTests.cs' @'
using Xunit;

namespace Lib.Tests;

public class ReaderTests
{
    [Fact]
    public void ReadsTheField() { var counter = new Lib.Counter(); counter.Next(); Assert.Equal(1, Lib.Reader.Read(counter)); }
}
'@
        New-Solution $root 'Src/Fixture.sln' 'Src/Lib/Lib.csproj', 'Src/Lib.Tests/Lib.Tests.csproj'
    }
}
