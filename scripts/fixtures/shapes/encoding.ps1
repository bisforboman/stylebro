# Trials 49, 53, 54: the repository's charset and final-newline settings against a minority of files.
# - BaGet (trial 53): 'charset = utf-8' with a few files committed with a BOM; init turns IDE0055 on, and format strips them:
#   init announces it (ConvertedMinorityFileNote), format changes exactly those.
# - Dapper (trial 54): 'charset = utf-8-bom' with one file without a BOM; 'insert_final_newline = true' with files without a
#   final line break.
@{
    Description = 'BOM and final-newline minorities against the repository''s .editorconfig'
    InitReason = 'BOM|byte order mark'
    Create = {
        param($root)
        Set-Text $root '.editorconfig' @'
root = true

[*.cs]
charset = utf-8
insert_final_newline = true

[tests/**.cs]
charset = utf-8-bom
'@
        New-Project $root 'src/Lib/Lib.csproj'
        foreach ($name in 'Alpha', 'Beta', 'Gamma', 'Delta') { Set-Messy $root "src/Lib/$name.cs" -Namespace 'Lib' }
        Set-Messy $root 'src/Lib/WithBom.cs' -Namespace 'Lib' -Bom
        Set-Text $root 'src/Lib/NoFinalNewline.cs' -NoFinalNewline "namespace Lib;`n`npublic static class NoFinalNewline`n{`n    public const int Value = 1;`n}"
        New-TestProject $root 'tests/Lib.Tests/Lib.Tests.csproj' -References '../../src/Lib/Lib.csproj'
        foreach ($name in 'OneTests', 'TwoTests', 'ThreeTests') {
            Set-Text $root "tests/Lib.Tests/$name.cs" -Bom "using Xunit;`n`nnamespace Lib.Tests;`n`npublic class $name`n{`n    [Fact]`n    public void Works() { Assert.Equal(1, Lib.NoFinalNewline.Value); }`n}"
        }
        Set-Text $root 'tests/Lib.Tests/NoBomTests.cs' "using Xunit;`n`nnamespace Lib.Tests;`n`npublic class NoBomTests`n{`n    [Fact]`n    public void Works() { Assert.Equal(1, Lib.NoFinalNewline.Value); }`n}"
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'tests/Lib.Tests/Lib.Tests.csproj'
    }
}
