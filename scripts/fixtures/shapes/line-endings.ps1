# Trials 43, 51, 52: line endings.
# - error-or (trial 52): most files CRLF, a minority LF; init turns IDE0055 on, format rewrites the minority: init announces it
#   (ConvertedMinorityNote) and format changes exactly those.
# - esprima (trial 51): a CRLF file with a multi-line verbatim string, whose text the tests compare: converting it would change
#   the string, so it keeps its line endings and the test still passes.
# - A lone CR inside a comment, which neither convention describes.
# No .gitattributes: the checkout has what was committed.
@{
    Description = 'CRLF majority, an LF minority, a CRLF verbatim string, a lone CR'
    InitReason = 'LF'
    Create = {
        param($root)
        New-Project $root 'src/Lib/Lib.csproj'
        foreach ($name in 'Alpha', 'Beta', 'Gamma', 'Delta') { Set-Messy $root "src/Lib/$name.cs" -Namespace 'Lib' -Crlf }
        Set-Messy $root 'src/Lib/Minority.cs' -Namespace 'Lib'
        Set-Text $root 'src/Lib/Verbatim.cs' -Crlf @'
namespace Lib;

public static class Verbatim
{
    public static string Text = @"first
second";
}
'@
        Set-Text $root 'src/Lib/LoneCr.cs' -Crlf ("namespace Lib;`n`n// a lone CR here:`r and the comment goes on`npublic static class LoneCr`n{`n    static public int Count;`n}")
        New-TestProject $root 'tests/Lib.Tests/Lib.Tests.csproj' -References '../../src/Lib/Lib.csproj'
        Set-Text $root 'tests/Lib.Tests/VerbatimTests.cs' -Crlf @'
using Xunit;

namespace Lib.Tests;

public class VerbatimTests
{
    [Fact]
    public void KeepsItsLineBreak() { Assert.Equal("first\r\nsecond", Lib.Verbatim.Text); }
}
'@
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'tests/Lib.Tests/Lib.Tests.csproj'
    }
}
