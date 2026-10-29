# Trial 62 (jbevain/mono.linq.expressions): C# indented with tabs and no .editorconfig. init wrote no indent_style, so the
# preset's spaces applied: BRO1107 indented the lines it added with spaces (and with IDE0055 on, the whitespace pass would
# have turned every tab into spaces). init now writes indent_style = tab, and every indented line stays tab-indented.
@{
    Description = 'tab-indented code without an .editorconfig'
    InitReason = 'indented with tabs'
    Assert = {
        param($repo)
        foreach ($file in Get-ChildItem -Recurse -Filter *.cs (Join-Path $repo 'src'), (Join-Path $repo 'tests') | Where-Object { $_.FullName -notmatch '[\\/](obj|bin)[\\/]' }) {
            if ([IO.File]::ReadAllText($file.FullName) -match '(?m)^ +\S') { "$($file.Name) has lines indented with spaces" }
        }
    }
    Create = {
        param($root)
        New-Project $root 'src/Lib/Lib.csproj'
        Set-Text $root 'src/Lib/Calc.cs' "namespace Lib;`n`npublic class Calc`n{`n`tpublic static int Twice(int x) { return x * 2; }`n`n`tpublic static int Sum(int a,`n`t`tint b)`n`t{`n`t`treturn a + b;`n`t}`n}`n"
        foreach ($name in 'Alpha', 'Beta') { Set-Text $root "src/Lib/$name.cs" "namespace Lib;`n`npublic static class $name`n{`n`tpublic const int Value = 1;`n`n`tpublic static int Get() => Value;`n}`n" }
        New-TestProject $root 'tests/Lib.Tests/Lib.Tests.csproj' -References '../../src/Lib/Lib.csproj'
        Set-Text $root 'tests/Lib.Tests/CalcTests.cs' "using Xunit;`n`nnamespace Lib.Tests;`n`npublic class CalcTests`n{`n`t[Fact]`n`tpublic void Twice() { Assert.Equal(4, Lib.Calc.Twice(2)); }`n`n`t[Fact]`n`tpublic void Sum() { Assert.Equal(3, Lib.Calc.Sum(1, 2)); }`n}`n"
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'tests/Lib.Tests/Lib.Tests.csproj'
    }
}
