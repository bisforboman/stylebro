# Path B (docs/migrating.md) in its most common modern shape: StyleCop.Analyzers.Unstable as a GlobalPackageReference under
# central package management, stylecop.json, a few StyleCop defaults changed in .editorconfig (trials 33, 48, 56). The
# preview adds StyleBro next to StyleCop; the user then runs 'migrate --write' and swaps the package. A StyleCop-clean
# repository: format should change close to nothing, and nothing StyleCop was happy with.
@{
    Description = 'StyleCop 1.2 through a GlobalPackageReference, central package management, migrate'
    Migrate = $true
    Reference = {
        param($repo, $context)
        $file = Join-Path $repo 'Directory.Packages.props'
        $text = (Get-Content -Raw $file).Replace('<GlobalPackageReference Include="StyleCop.Analyzers.Unstable" Version="1.2.0.556" />', "<GlobalPackageReference Include=`"StyleBro.Analyzers`" Version=`"$($context.Version)`" />")
        Set-Text $repo 'Directory.Packages.props' $text
    }
    Create = {
        param($root)
        Set-Text $root 'Directory.Packages.props' @'
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <GlobalPackageReference Include="StyleCop.Analyzers.Unstable" Version="1.2.0.556" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageVersion Include="xunit" Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
</Project>
'@
        Set-Text $root 'stylecop.json' '{ "settings": { "orderingRules": { "usingDirectivesPlacement": "outsideNamespace" }, "documentationRules": { "companyName": "Fixture" } } }'
        Set-Text $root 'Directory.Build.props' @'
<Project>
  <ItemGroup>
    <AdditionalFiles Include="$(MSBuildThisFileDirectory)stylecop.json" Link="stylecop.json" />
  </ItemGroup>
</Project>
'@
        Set-Text $root '.editorconfig' @'
root = true

[*.cs]
dotnet_diagnostic.SA1101.severity = none
dotnet_diagnostic.SA1309.severity = none
dotnet_diagnostic.SA1600.severity = none
dotnet_diagnostic.SA1633.severity = none
dotnet_diagnostic.SA1413.severity = none
'@
        Set-Text $root 'src/Lib/Lib.csproj' "<Project Sdk=`"Microsoft.NET.Sdk`">`n  <PropertyGroup>`n    <TargetFramework>net10.0</TargetFramework>`n  </PropertyGroup>`n</Project>"
        Set-Text $root 'src/Lib/Counter.cs' @'
namespace Lib
{
    using System;

    public class Counter
    {
        private int _count;

        public Counter(int start)
        {
            _count = start;
        }

        public int Next()
        {
            if (_count == int.MaxValue)
            {
                throw new InvalidOperationException("Full");
            }

            return ++_count;
        }
    }
}
'@
        Set-Text $root 'tests/Lib.Tests/Lib.Tests.csproj' @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/Lib/Lib.csproj" />
  </ItemGroup>
</Project>
'@
        Set-Text $root 'tests/Lib.Tests/CounterTests.cs' @'
namespace Lib.Tests
{
    using Xunit;

    public class CounterTests
    {
        [Fact]
        public void Next()
        {
            var counter = new Counter(1);
            Assert.Equal(2, counter.Next());
        }
    }
}
'@
        New-Solution $root 'Fixture.slnx' 'src/Lib/Lib.csproj', 'tests/Lib.Tests/Lib.Tests.csproj'
    }
}
