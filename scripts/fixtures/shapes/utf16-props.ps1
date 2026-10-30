# Trial 62 (jbevain/mono.linq.expressions): Directory.Build.props committed as UTF-16 with a byte order mark and CRLF. init
# read it as UTF-8 and wrote the preset version back as UTF-8 with LF: the whole file changed (git showed a binary diff).
# The step 1 edit keeps the encoding, as an editor would; init must keep it too.
@{
    Description = 'a UTF-16 Directory.Build.props with CRLF'
    Reference = {
        param($repo, $context)
        $path = Join-Path $repo 'Directory.Build.props'
        $item = "  <ItemGroup>`r`n    <PackageReference Include=`"StyleBro.Analyzers`" Version=`"$($context.Version)`" PrivateAssets=`"all`" />`r`n  </ItemGroup>`r`n"
        [IO.File]::WriteAllText($path, [IO.File]::ReadAllText($path).Replace('</Project>', "$item</Project>"), [Text.Encoding]::Unicode)
    }
    Assert = {
        param($repo)
        $bytes = [IO.File]::ReadAllBytes((Join-Path $repo 'Directory.Build.props'))
        $text = [IO.File]::ReadAllText((Join-Path $repo 'Directory.Build.props'))
        if ($bytes.Length -lt 2 -or $bytes[0] -ne 0xFF -or $bytes[1] -ne 0xFE) { 'Directory.Build.props isn''t UTF-16 with a BOM any more' }
        if ($text -match '[^\r]\n') { 'Directory.Build.props has LF line endings now' }
        if ($text -notmatch '<StyleBroPreset>') { 'init didn''t set the preset version in Directory.Build.props' }
    }
    Create = {
        param($root)
        $props = "<Project>`r`n  <PropertyGroup>`r`n    <Deterministic>true</Deterministic>`r`n  </PropertyGroup>`r`n</Project>`r`n"
        [IO.File]::WriteAllText((Join-Path $root 'Directory.Build.props'), $props, [Text.Encoding]::Unicode)
        New-Project $root 'src/Lib/Lib.csproj'
        Set-Messy $root 'src/Lib/Calc.cs' -Namespace 'Lib'
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
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'tests/Lib.Tests/Lib.Tests.csproj'
    }
}
