# Trials 31, 52, 53: a folder with a .nupkg was taken for an extracted package and skipped.
# - DbUp (trial 52): src/dbup.1.2.3.nupkg next to src/DbUp.sln and src/Directory.Build.props, none at the root: init read no
#   code and pinned the preset in a root props nothing imports; check said "Clean" with findings left.
# - BaGet (trial 53): tests/.../TestData/TestData.1.2.3.nupkg next to ordinary code and no project: format fixed the file and
#   said "0 files changed".
# - A real packages.config folder (packages/<id>.<version>/<id>.<version>.nupkg) is still a package: its .cs content is left
#   alone.
@{
    Description = 'a .nupkg next to the solution, one next to test code, a packages/ folder'
    Reference = {
        param($repo)
        $props = Join-Path $repo 'src/Directory.Build.props'
        $item = "  <ItemGroup>`n    <PackageReference Include=`"StyleBro.Analyzers`" Version=`"$($args[0].Version)`" PrivateAssets=`"all`" />`n  </ItemGroup>`n"
        Set-Text $repo 'src/Directory.Build.props' ((Get-Content -Raw $props).Replace('</Project>', "$item</Project>"))
    }
    Assert = {
        param($repo)
        if ((Get-Content -Raw (Join-Path $repo 'src/packages/Old.Thing.1.0.0/content/Thing.cs')) -notmatch 'static public') { 'the extracted package''s content file was formatted' }
        if ((Get-Content -Raw (Join-Path $repo 'src/Lib.Tests/TestData/Sample.cs')) -match 'static public') { 'the code next to TestData.1.2.3.nupkg was not formatted' }
        if (Test-Path (Join-Path $repo 'Directory.Build.props')) { 'init wrote a root Directory.Build.props the projects under src/ don''t import' }
    }
    Create = {
        param($root)
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        function Zip([string]$path) {
            $full = Join-Path $root $path
            New-Item -ItemType Directory -Force (Split-Path $full -Parent) | Out-Null
            $zip = [IO.Compression.ZipFile]::Open($full, 'Create')
            try { $entry = $zip.CreateEntry('readme.txt'); $w = [IO.StreamWriter]::new($entry.Open()); $w.Write('fixture'); $w.Dispose() } finally { $zip.Dispose() }
        }
        Set-Text $root 'src/Directory.Build.props' "<Project>`n  <PropertyGroup>`n    <Nullable>enable</Nullable>`n  </PropertyGroup>`n</Project>"
        Zip 'src/dbup.1.2.3.nupkg'
        New-Project $root 'src/Lib/Lib.csproj'
        Set-Messy $root 'src/Lib/Calc.cs' -Namespace 'Lib'
        New-TestProject $root 'src/Lib.Tests/Lib.Tests.csproj' -References '../Lib/Lib.csproj'
        Zip 'src/Lib.Tests/TestData/TestData.1.2.3.nupkg'
        Set-Messy $root 'src/Lib.Tests/TestData/Sample.cs' -Namespace 'Lib.Tests'
        Set-Text $root 'src/Lib.Tests/CalcTests.cs' @'
using Xunit;

namespace Lib.Tests;

public class CalcTests
{
    [Fact]
    public void Twice() { Assert.Equal(4, Lib.Calc.Twice(2)); }
}
'@
        Zip 'src/packages/Old.Thing.1.0.0/Old.Thing.1.0.0.nupkg'
        Set-Messy $root 'src/packages/Old.Thing.1.0.0/content/Thing.cs' -Namespace 'Old'
        Set-Text $root 'src/Lib/Lib.csproj' ((Get-Content -Raw (Join-Path $root 'src/Lib/Lib.csproj')).Replace('</Project>', "  <ItemGroup>`n    <Compile Remove=`"..\packages\**`" />`n  </ItemGroup>`n</Project>"))
        New-Solution $root 'src/Fixture.sln' 'src/Lib/Lib.csproj', 'src/Lib.Tests/Lib.Tests.csproj'
    }
}
