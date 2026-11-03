# Trial 66 (moq4): a Directory.Build.targets drops net48 off Windows, so a project's <TargetFrameworks>net48;net10.0</TargetFrameworks>
# evaluates to a single "net10.0" (an outer build with one inner build). With a warning from the design-time build the host
# probes each project of the run for a failed design-time build; it built this one without a framework, got the outer build
# ("The target CoreCompile does not exist") and said "Not checked" (exit 3) for projects that had loaded and been checked.
@{
    Description = 'a single entry left in <TargetFrameworks>, with a design-time warning'
    Create = {
        param($root)
        # Windows keeps net48 (the project then has two frameworks, a shape the other fixtures cover).
        Set-Text $root 'Directory.Build.targets' @'
<Project>
  <PropertyGroup Condition="'$(OS)' != 'Windows_NT'">
    <TargetFrameworks Condition="$(TargetFrameworks.Contains('net48;'))">$(TargetFrameworks.Replace('net48;', ''))</TargetFrameworks>
  </PropertyGroup>
  <Target Name="WarnInDesignTime" BeforeTargets="CoreCompile" Condition="'$(DesignTimeBuild)' == 'true'">
    <Warning Code="FIX0001" Text="A warning only the design-time build logs (fixture)." />
  </Target>
</Project>
'@
        New-Project $root 'src/Lib/Lib.csproj'
        Set-Messy $root 'src/Lib/Calc.cs' -Namespace 'Lib'
        New-Project $root 'src/App/App.csproj' -Frameworks 'net48;net10.0' -References '../Lib/Lib.csproj'
        Set-Messy $root 'src/App/Thing.cs' -Namespace 'App'
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'src/App/App.csproj'
    }
    Tests = $false
}
