# Trial 57 (DapperAOT): one project's design-time build fails (there: a negative git height); it loaded without its settings,
# reported nothing, and only a load-warning note said anything. Such a project is "Not checked" with MSBuild's error, and the
# verdict Not verified. The real build passes: the failure is design-time only.
@{
    Description = 'one project whose design-time build fails'
    Expect = @{ preview = 3; format = 3; check = 3; 'check-build' = 3 }
    Reasons = @{ format = 'Not checked.*Broken|Broken.*(fail|error)'; check = 'Broken' }
    Create = {
        param($root)
        New-Project $root 'src/Lib/Lib.csproj'
        Set-Messy $root 'src/Lib/Calc.cs' -Namespace 'Lib'
        New-Project $root 'src/Broken/Broken.csproj' -References '../Lib/Lib.csproj' -Extra @'
  <Target Name="FailInDesignTime" BeforeTargets="CoreCompile" Condition="'$(DesignTimeBuild)' == 'true'">
    <Error Text="Broken can't be evaluated outside a real build (fixture)." />
  </Target>
'@
        Set-Messy $root 'src/Broken/Thing.cs' -Namespace 'Broken'
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'src/Broken/Broken.csproj'
    }
    Tests = $false
}
