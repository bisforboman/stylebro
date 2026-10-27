# Trial 57 (DapperAOT): a depth-1 clone (CI's checkout) of a Nerdbank.GitVersioning repository whose version needs more
# history than the clone has. Format then check said "Clean." with 22 files of findings left: the design-time build failed and
# the projects loaded without their settings. Now such projects are "Not checked" with MSBuild's error, Not verified (3).
# The repository's own build fails in such a clone, before and after StyleBro.
@{
    Description = 'Nerdbank.GitVersioning in a depth-1 clone'
    Shallow = $true
    Expect = @{ preview = 3; format = 3; check = 3; 'check-build' = 3 }
    Reasons = @{ format = 'Not checked'; check = 'Not checked' }
    BuildFails = 'Nerdbank.GitVersioning needs more history than a shallow clone has'
    Create = {
        param($root)
        Set-Text $root 'version.json' '{ "version": "1.0", "versionHeightOffset": -4 }'
        Set-Text $root 'Directory.Build.props' @'
<Project>
  <ItemGroup>
    <PackageReference Include="Nerdbank.GitVersioning" Version="3.7.115" PrivateAssets="all" />
  </ItemGroup>
</Project>
'@
        New-Project $root 'src/Lib/Lib.csproj'
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj'
        foreach ($i in 1..5) {
            Set-Messy $root 'src/Lib/Calc.cs' -Namespace 'Lib'
            Set-Text $root 'src/Lib/History.txt' "commit $i"
            Save-Commit $root "history $i"
        }
    }
}
