# Trials 29 and 43 (Moq, NSwag's NoInstaller.slnf): users name a solution filter. Its test project references a project the
# filter leaves out (but the solution lists): building the filter builds that project too, so format must check it, or the
# TWAE build of the filter fails after a clean check.
@{
    Description = 'a solution filter whose test project references a project outside the filter'
    Args = @('Fixture.slnf')
    PreviewArgs = @('--project', 'Fixture.slnf')
    Target = 'Fixture.slnf'
    Create = {
        param($root)
        New-Project $root 'src/Lib/Lib.csproj'
        Set-Messy $root 'src/Lib/Calc.cs' -Namespace 'Lib'
        New-Project $root 'src/Extra/Extra.csproj' -References '../Lib/Lib.csproj'
        Set-Messy $root 'src/Extra/Probe.cs' -Namespace 'Extra'
        New-Project $root 'src/Installer/Installer.csproj'
        Set-Messy $root 'src/Installer/Setup.cs' -Namespace 'Installer'
        New-TestProject $root 'tests/Lib.Tests/Lib.Tests.csproj' -References '../../src/Lib/Lib.csproj', '../../src/Extra/Extra.csproj'
        Set-Text $root 'tests/Lib.Tests/CalcTests.cs' @'
using Xunit;

namespace Lib.Tests;

public class CalcTests
{
    [Fact]
    public void Twice() { Assert.Equal(4, Extra.Probe.Twice(2)); }
}
'@
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'src/Extra/Extra.csproj', 'src/Installer/Installer.csproj', 'tests/Lib.Tests/Lib.Tests.csproj'
        Set-Text $root 'Fixture.slnf' @'
{
  "solution": {
    "path": "Fixture.sln",
    "projects": [
      "src/Lib/Lib.csproj",
      "tests/Lib.Tests/Lib.Tests.csproj"
    ]
  }
}
'@
    }
}
