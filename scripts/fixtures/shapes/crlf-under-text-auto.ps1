# Trials 43 and 51 (NSwag, esprima): '* text=auto' added after files were committed with CRLF. The checkout has CRLF for
# them; the preview's copy once stored them as LF (git renormalizes on add), so init counted them as converted and the patch
# made a multi-line string LF: applied, a test failed. The copy marks such files -text (PreviewCommand.CommittedWithCrLf).
@{
    Description = "files committed with CRLF before '* text=auto'"
    Create = {
        param($root)
        New-Project $root 'src/Lib/Lib.csproj'
        Set-Text $root 'src/Lib/Verbatim.cs' -Crlf @'
namespace Lib;

public static class Verbatim
{
    public static string Text = @"first
second";

    static public int Count;
}
'@
        foreach ($name in 'Alpha', 'Beta') { Set-Messy $root "src/Lib/$name.cs" -Namespace 'Lib' }
        New-TestProject $root 'tests/Lib.Tests/Lib.Tests.csproj' -References '../../src/Lib/Lib.csproj'
        Set-Text $root 'tests/Lib.Tests/VerbatimTests.cs' @'
using Xunit;

namespace Lib.Tests;

public class VerbatimTests
{
    [Fact]
    public void KeepsItsLineBreak() { Assert.Equal("first\r\nsecond", Lib.Verbatim.Text); }
}
'@
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'tests/Lib.Tests/Lib.Tests.csproj'
        Save-Commit $root 'before .gitattributes'
        Set-Text $root '.gitattributes' '* text=auto'
    }
}
