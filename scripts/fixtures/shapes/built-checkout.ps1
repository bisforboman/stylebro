# Trials 42 and 56: MSBuild's query mode (-getProperty without a target) evaluates what a build skips.
# - PolyShim (trial 42): VersionGreaterThanOrEquals($(TargetFrameworkVersion), '4.5') in an item Condition under a
#   .NETFramework-only group failed the outer evaluation of a multi-targeted project (MSB4184).
# - DynamicData (trial 56): Regex.Replace(%(Filename), ...) in DependentUpon metadata; once the checkout had been built, the
#   outer evaluation got '.NETCoreApp,Version=vX.AssemblyAttributes', whose comma split the arguments (MSB4186).
# The checkout is built before StyleBro runs, as a developer's is.
@{
    Description = 'item functions on %(Filename), a .NETFramework-only condition, an already-built checkout'
    Before = {
        param($repo)
        dotnet build $repo --nologo -v q | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'The untouched fixture does not build.' }
    }
    Create = {
        param($root)
        Set-Text $root 'src/Directory.Build.targets' @'
<Project>
  <ItemGroup>
    <!-- Automatic file nesting, for .cs files (DynamicData's) -->
    <Compile Update="**\*.*.cs">
      <DependentUpon>$([System.Text.RegularExpressions.Regex]::Replace(%(Filename), '\.[^\.]+$', '.cs'))</DependentUpon>
    </Compile>
  </ItemGroup>
  <ItemGroup Condition="'$(TargetFrameworkIdentifier)' == '.NETFramework'">
    <Reference Include="System.Net.Http" Condition="$([MSBuild]::VersionGreaterThanOrEquals($(TargetFrameworkVersion), '4.5'))" />
  </ItemGroup>
</Project>
'@
        New-Project $root 'src/Lib/Lib.csproj' -Frameworks 'net8.0;net10.0'
        Set-Messy $root 'src/Lib/Calc.cs' -Namespace 'Lib'
        Set-Text $root 'src/Lib/Calc.Extra.cs' "namespace Lib;`n`npublic static class CalcExtra`n{`n    public static int Thrice(int x) => x * 3;`n}"
        New-TestProject $root 'src/Lib.Tests/Lib.Tests.csproj' -Frameworks 'net8.0;net10.0' -References '../Lib/Lib.csproj'
        Set-Text $root 'src/Lib.Tests/Calc.Tests.cs' @'
using Xunit;

namespace Lib.Tests;

public class CalcTests
{
    [Fact]
    public void Thrice() { Assert.Equal(6, Lib.CalcExtra.Thrice(2)); }
}
'@
        New-Solution $root 'Fixture.sln' 'src/Lib/Lib.csproj', 'src/Lib.Tests/Lib.Tests.csproj'
    }
}
