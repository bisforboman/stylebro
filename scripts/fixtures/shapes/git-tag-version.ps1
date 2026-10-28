# Trial 59 (SharpYaml): MinVer versions the assemblies from the git tags, and the untouched Debug build has an error whose
# text names an assembly's version (CS0281 there). check --build's copy of HEAD had the history but not the tags, built as
# 0.0.0.0, so the same error read as new and check --build said "Not clean". Here App misses Core's reference in Debug
# (no transitive project references): CS0012 names 'Core, Version=1.0.0.0', untouched too.
@{
    Description = 'MinVer versions from a git tag, an untouched Debug error naming that version (CS0012)'
    BuildFails = "CS0012 in Debug: App doesn't reference Core, untouched too"
    Create = {
        param($root)
        Set-Text $root 'Directory.Build.props' @'
<Project>
  <PropertyGroup>
    <MinVerTagPrefix>v</MinVerTagPrefix>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MinVer" Version="7.0.0" PrivateAssets="all" />
  </ItemGroup>
</Project>
'@
        New-Project $root 'src/Core/Core.csproj'
        Set-Text $root 'src/Core/Thing.cs' "namespace Core;`n`npublic class Thing`n{`n}"
        New-Project $root 'src/Lib/Lib.csproj' -References '../Core/Core.csproj'
        Set-Text $root 'src/Lib/Maker.cs' "namespace Lib;`n`npublic static class Maker`n{`n    public static Core.Thing Make() => new Core.Thing();`n}"
        Set-Messy $root 'src/Lib/Calc.cs' -Namespace 'Lib'
        New-Project $root 'src/App/App.csproj' -Properties ([ordered]@{ OutputType = 'Exe' }) -References '../Lib/Lib.csproj' `
            -Extra "  <PropertyGroup Condition=`"'`$(Configuration)' == 'Debug'`">`n    <DisableTransitiveProjectReferences>true</DisableTransitiveProjectReferences>`n  </PropertyGroup>"
        Set-Text $root 'src/App/Program.cs' "var thing = Lib.Maker.Make();`nSystem.Console.WriteLine(thing);"
        New-Solution $root 'Fixture.slnx' 'src/Core/Core.csproj', 'src/Lib/Lib.csproj', 'src/App/App.csproj'
        Save-Commit $root 'release'
        git -C $root tag v1.2.3
    }
    Assert = {
        param($formatted, $runs, $context)
        foreach ($engine in $runs.Keys) {
            $text = $runs[$engine].Steps['check-build'].Text
            # The Debug build must still fail (else the shape tests nothing) with errors counted as the untouched code's.
            if ($text -notmatch 'Debug: no findings, no new errors \(\d+ errors? the untouched code has too') { "$engine check --build didn't count Debug's CS0012 as an error the untouched code has too:`n$text" }
        }
    }
    Tests = $false
}
