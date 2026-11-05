# Trial 68 (CacheManager): a test project writes <TargetFrameworks> over several lines with a commented-out entry, which
# evaluates to ";net10.0" plus the line breaks, so its target framework is "net10.0<line break>    ". 'dotnet format' then
# printed "Required references did not load for Lib(net10.0<line break>    ) or referenced project" over two lines, which
# format read line by line (the host's one message with a line break in it was not matched by a '.' either): the skip went
# unseen, the project's analyzers never ran and format, check and --json said Clean while the build reported its findings.
# Such a project is "Not checked", the verdict Not verified; check --build still reports what the build sees.
# The old engine (--engine dotnet-format) rewrites <TargetFrameworks> to the run's framework, so the line breaks never reach
# MSBuild's inner builds and it formats the project: the engines differ here (open question for the owner: should the host
# do the same?). Two parts of the host: the skip message has a line break in it, and the project's instance name
# 'Spread(net10.0<line break>    )' matched none of the runs' frameworks, so the project was in no run at all.
@{
    Description = 'a project whose <TargetFrameworks> has line breaks in it'
    Expect = @{ preview = 3; format = 3; check = 3; 'check-build' = 2 }
    Reasons = @{ format = 'Not checked.*Spread'; check = 'Spread' }
    EnginesDiffer = 'the old engine rewrites <TargetFrameworks> per run and formats the project; the host reports it as not checked'
    EngineExpect = @{ 'dotnet-format' = @{} }
    EngineReasons = @{ 'dotnet-format' = @{} }
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
