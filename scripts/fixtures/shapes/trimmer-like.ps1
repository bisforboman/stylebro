# Trial 54 (Dapper): in a run for a framework the library doesn't have, NuGet's nuget.g.props saw one framework (none of its
# per-framework ImportGroups matched) and nuget.g.targets another, so the outer build imported ReferenceTrimmer's
# buildMultiTargeting targets without the props that define its task assembly: the library didn't load (format Not verified,
# check --build Not clean, both honest). Fixture.Trimmer (scripts/fixtures/packages) has that shape.
@{
    Description = 'a library missing the run''s framework, whose package brings props and targets'
    Create = {
        param($root, $context)
        New-Project $root 'src/Lib/Lib.csproj' -Frameworks 'netstandard2.0;net10.0' -Properties ([ordered]@{ LangVersion = 'latest' }) -Packages ([ordered]@{ 'Fixture.Trimmer' = $context.Version }) -Extra @'
  <ItemGroup>
    <PackageReference Update="Fixture.Trimmer" PrivateAssets="all" />
  </ItemGroup>
'@
        Set-Messy $root 'src/Lib/Calc.cs' -Namespace 'Lib'
        New-TestProject $root 'tests/Lib.Tests/Lib.Tests.csproj' -Frameworks 'net8.0;net10.0' -References '../../src/Lib/Lib.csproj'
        Set-Text $root 'tests/Lib.Tests/CalcTests.cs' @'
using Xunit;

namespace Lib.Tests;

public class CalcTests
{
    [Fact]
    public void Twice() { Assert.Equal(4, Lib.Calc.Twice(2)); }
}
'@
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'tests/Lib.Tests/Lib.Tests.csproj'
    }
}
