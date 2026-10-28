# Trials 50, 51, 52 (Dahomey.Json, AsyncEx, EasyNetQ): src/ and tests/ have their own Directory.Build.props, the root none.
# 'bro preview format <project>' once wrote a Directory.Build.props into the project's folder, which hid src/'s (TWAE, docs);
# the user's step 1 puts the reference into the props the projects already import.
# Trial 59 (DelegateDecompiler): the reference in a root Directory.Build.props the nested ones shadow; format must name where it goes.
@{
    Description = 'src/ and tests/ Directory.Build.props, none at the root'
    Reference = {
        param($repo, $context)
        foreach ($file in 'src/Directory.Build.props', 'tests/Directory.Build.props') {
            $item = "  <ItemGroup>`n    <PackageReference Include=`"StyleBro.Analyzers`" Version=`"$($context.Version)`" PrivateAssets=`"all`" />`n  </ItemGroup>`n"
            Set-Text $repo $file ((Get-Content -Raw (Join-Path $repo $file)).Replace('</Project>', "$item</Project>"))
        }
    }
    Assert = {
        param($repo, $runs, $context)
        # The preview of one project, on the formatted and committed code: it must not add a props file that hides src/'s.
        git -C $repo -c user.name=f -c user.email=f@example.com commit -qam formatted | Out-Null
        Push-Location $repo
        try { $out = & $context.Bro preview format src/Lib/Lib.csproj 2>&1 | Out-String } finally { Pop-Location }
        $patch = Join-Path $repo '.git/stylebro-preview.patch'
        if ((Test-Path $patch) -and (Get-Content -Raw $patch) -match 'b/src/Lib/Directory\.Build\.props') { "'bro preview format src/Lib/Lib.csproj' added src/Lib/Directory.Build.props, which hides src/Directory.Build.props" }
        if ($LASTEXITCODE -ne 0) { "'bro preview format src/Lib/Lib.csproj' exited $LASTEXITCODE`n$out" }

        # Trial 59 (DelegateDecompiler): the reference in a new root Directory.Build.props, which src/'s and tests/' shadow.
        # Format stops (nothing loads StyleBro) and must name the file the reference belongs in, not just "add the package".
        $misplaced = Join-Path ([IO.Path]::GetTempPath()) "fixture-misplaced-$([guid]::NewGuid().ToString('N').Substring(0, 8))"
        git clone -q $repo $misplaced
        try {
            foreach ($file in 'src/Directory.Build.props', 'tests/Directory.Build.props') {
                Set-Text $misplaced $file ((Get-Content -Raw (Join-Path $misplaced $file)) -replace '(?s)\s*<ItemGroup>\s*<PackageReference Include="StyleBro\.Analyzers"[^>]*/>\s*</ItemGroup>', '')
            }
            Set-Text $misplaced 'Directory.Build.props' "<Project>`n  <ItemGroup>`n    <PackageReference Include=`"StyleBro.Analyzers`" Version=`"$($context.Version)`" PrivateAssets=`"all`" />`n  </ItemGroup>`n</Project>"
            Push-Location $misplaced
            try { $out = & $context.Bro format 2>&1 | Out-String; $code = $LASTEXITCODE } finally { Pop-Location }
            if ($code -ne 1) { "'bro format' with the reference only in the root Directory.Build.props exited $code (expected 1)`n$out" }
            elseif ($out -notmatch 'put the reference in (src|tests)/Directory\.Build\.props') { "'bro format' with the reference only in the root Directory.Build.props didn't name src/ or tests/ Directory.Build.props:`n$out" }
        }
        finally { Remove-Item -Recurse -Force $misplaced -ErrorAction SilentlyContinue }
    }
    Create = {
        param($root)
        Set-Text $root 'src/Directory.Build.props' "<Project>`n  <PropertyGroup>`n    <GenerateDocumentationFile>true</GenerateDocumentationFile>`n    <NoWarn>`$(NoWarn);CS1591</NoWarn>`n  </PropertyGroup>`n</Project>"
        Set-Text $root 'tests/Directory.Build.props' "<Project>`n  <PropertyGroup>`n    <IsPackable>false</IsPackable>`n  </PropertyGroup>`n</Project>"
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
