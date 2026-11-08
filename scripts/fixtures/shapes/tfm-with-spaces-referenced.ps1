# Trial 69 (Arch): a library writes <TargetFrameworks>net8.0; netstandard2.1</TargetFrameworks> (spaces after the ';') and
# single-framework projects reference it. Trial 68's trim of white space in the list (the injected targets file) returned
# it with its ';' escaped, so the SDK's inner builds became one ('TargetFramework=net8.0;netstandard2.1', Unsupported) and
# the referencing projects didn't load on the host ("cannot be referenced by a project that targets
# '.NETCoreApp,Version=v8.0'"): format said Incomplete and left them alone. The trim now unescapes its result.
@{
    Description = 'a library with spaces in <TargetFrameworks> that another project references'
    Assert = {
        param($formatted, $runs, $context)
        foreach ($engine in $runs.Keys) {
            foreach ($file in 'src/Lib/Calc.cs', 'src/App/Thing.cs') {
                if ((Get-Content -Raw (Join-Path $runs[$engine].Clone $file)) -notmatch 'public static int Twice\(') { "$engine did not format $file" }
            }
        }
    }
    Target = 'Fixture.sln'
    Create = {
        param($root)
        Set-Text $root 'src/Lib/Lib.csproj' @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net10.0; netstandard2.1</TargetFrameworks>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>
'@
        Set-Messy $root 'src/Lib/Calc.cs' -Namespace 'Lib'
        Set-Text $root 'src/App/App.csproj' @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Lib/Lib.csproj" />
  </ItemGroup>
</Project>
'@
        Set-Messy $root 'src/App/Thing.cs' -Namespace 'App'
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'src/App/App.csproj'
    }
    Tests = $false
}
