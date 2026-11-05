# Trial 68 (CacheManager): a test project writes <TargetFrameworks> over several lines with a commented-out entry, which
# evaluates to ";net10.0" plus the line breaks, so its target framework is "net10.0<line break>    ". 'dotnet format' then
# printed "Required references did not load for Lib(net10.0<line break>    ) or referenced project" over two lines, which
# format read line by line (the host's one message with a line break in it was not matched by a '.' either): the skip went
# unseen, the project's analyzers never ran and format, check and --json said Clean while the build reported its findings.
# Such a project is "Not checked", the verdict Not verified; check --build still reports what the build sees.
@{
    Description = 'a project whose <TargetFrameworks> has line breaks in it'
    Expect = @{ preview = 3; format = 3; check = 3; 'check-build' = 2 }
    Reasons = @{ format = 'Not checked.*Spread'; check = 'Spread' }
    Target = 'src/Other/Other.csproj'
    Create = {
        param($root)
        New-Project $root 'src/Other/Other.csproj'
        Set-Messy $root 'src/Other/Calc.cs' -Namespace 'Other'
        Set-Text $root 'src/Spread/Spread.csproj' @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>
      <!--net481-->;net10.0
    </TargetFrameworks>
  </PropertyGroup>
</Project>
'@
        Set-Messy $root 'src/Spread/Thing.cs' -Namespace 'Spread'
        New-Solution $root 'Fixture.sln' 'src/Other/Other.csproj', 'src/Spread/Spread.csproj'
    }
    Tests = $false
}
