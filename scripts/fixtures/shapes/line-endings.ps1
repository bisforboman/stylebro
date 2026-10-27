# Trials 43, 51, 52: line endings.
# - error-or (trial 52): most files CRLF, a minority LF; init turns IDE0055 on, format rewrites the minority: init announces it
#   (ConvertedMinorityNote) and format changes exactly those.
# - esprima (trial 51): a CRLF file with a multi-line verbatim string, whose text the tests compare: converting it would change
#   the string, so it keeps its line endings and the test still passes.
# - A lone CR inside a verbatim string (in a comment it would end the line: C# counts a CR as a line break), which neither
#   convention describes; the test checks it survives.
# No .gitattributes: the checkout has what was committed. The repository's .editorconfig says end_of_line = crlf.
@{
    Description = 'end_of_line = crlf, an LF minority, a CRLF verbatim string, a lone CR'
    InitReason = 'says end_of_line = crlf, and 1 of'
    Assert = {
        param($repo)
        if ([IO.File]::ReadAllText((Join-Path $repo 'src/Lib/Minority.cs')) -match '[^\r]\n') { 'the LF file wasn''t converted to CRLF as init announced' }
    }
    Create = {
        param($root)
        Set-Text $root '.editorconfig' "root = true`n`n[*]`nend_of_line = crlf"
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
        Set-Text $root 'src/Lib/LoneCr.cs' -Crlf ("namespace Lib;`n`npublic static class LoneCr`n{`n    public static string Text = @`"a`rb`";`n`n    static public int Count;`n}")
        New-TestProject $root 'tests/Lib.Tests/Lib.Tests.csproj' -References '../../src/Lib/Lib.csproj'
        Set-Text $root 'tests/Lib.Tests/VerbatimTests.cs' -Crlf @'
using Xunit;

namespace Lib.Tests;

public class VerbatimTests
{
    [Fact]
    public void KeepsItsLineBreak() { Assert.Equal("first\r\nsecond", Lib.Verbatim.Text); }

    [Fact]
    public void KeepsTheLoneCr() { Assert.Equal("a\rb", Lib.LoneCr.Text); }
}
'@
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'tests/Lib.Tests/Lib.Tests.csproj'
    }
}
