# Trial 68 (CacheManager): a test project writes <TargetFrameworks> over several lines with a commented-out entry, which
# evaluates to ";net10.0" plus the line breaks, so its target framework was "net10.0<line break>    ". The host loaded the
# project as 'Spread(net10.0<line break>    )', which matched none of the runs' frameworks: the project was in no run, its
# analyzers never ran and format, check and --json said Clean while the build reported its findings. (The old engine put the
# run's framework in the environment and formatted it.) bro now cuts the white space out of the list for its runs (the
# injected targets file), so both engines format the project, and a skip message with a line break in it is still read.
@{
    Description = 'a project whose <TargetFrameworks> has line breaks in it'
    Assert = {
        param($formatted, $runs, $context)
        foreach ($engine in $runs.Keys) {
            if ((Get-Content -Raw (Join-Path $runs[$engine].Clone 'src/Spread/Thing.cs')) -notmatch 'public static int Twice\(') { "$engine did not format the project with the multi-line TargetFrameworks" }
        }
    }
    Target = 'src/Other/Other.csproj'
    Create = {
        param($root)
        New-Project $root 'src/Other/Other.csproj' -Frameworks 'net8.0;net10.0'
        Set-Messy $root 'src/Other/Calc.cs' -Namespace 'Other'
        Set-Text $root 'src/Spread/Spread.csproj' @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>
      <!--net481-->;net10.0
    </TargetFrameworks>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Other/Other.csproj" />
  </ItemGroup>
</Project>
'@
        Set-Messy $root 'src/Spread/Thing.cs' -Namespace 'Spread'
        New-Solution $root 'Fixture.sln' 'src/Other/Other.csproj', 'src/Spread/Spread.csproj'
    }
    Tests = $false
}
