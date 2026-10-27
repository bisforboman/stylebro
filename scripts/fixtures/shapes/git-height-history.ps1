# Trial 57 (DapperAOT): Nerdbank.GitVersioning with a negative versionHeightOffset needs the repository's history; the
# preview's copy once had a history of its own (two commits), the version went negative, the design-time build stopped and
# the project loaded without its settings: the preview showed fewer files than format changed. A full clone must give the
# preview, format and check the same, clean results.
@{
    Description = 'Nerdbank.GitVersioning, versionHeightOffset -4, a full clone with history'
    Create = {
        param($root)
        Set-Text $root 'version.json' '{ "version": "1.0", "versionHeightOffset": -4 }'
        Set-Text $root 'Directory.Build.props' @'
<Project>
  <ItemGroup>
    <PackageReference Include="Nerdbank.GitVersioning" Version="3.7.115" PrivateAssets="all" />
  </ItemGroup>
</Project>
'@
        New-Project $root 'src/Lib/Lib.csproj'
        New-TestProject $root 'tests/Lib.Tests/Lib.Tests.csproj' -References '../../src/Lib/Lib.csproj'
        Set-Text $root 'tests/Lib.Tests/VersionTests.cs' @'
using Xunit;

namespace Lib.Tests;

public class VersionTests
{
    [Fact]
    public void HasAVersion() { Assert.NotNull(typeof(Lib.Calc).Assembly.GetName().Version); }
}
'@
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'tests/Lib.Tests/Lib.Tests.csproj'
        # Six commits: a height of 6, minus 4.
        foreach ($i in 1..5) {
            Set-Messy $root 'src/Lib/Calc.cs' -Namespace 'Lib'
            Set-Text $root 'src/Lib/History.txt' "commit $i"
            Save-Commit $root "history $i"
        }
    }
}
