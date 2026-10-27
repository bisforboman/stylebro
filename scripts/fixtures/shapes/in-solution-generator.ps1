# Trials 23, 42, 44, 45, 56: a source generator built in the same solution. format builds it first (its output is an
# analyzer of the other projects; the code using what it generates must compile), then checks everything. (Trials 44, 45
# and 56 saw "Not verified" for generator projects in Release configurations that differ from Debug; here they don't.)
@{
    Description = 'an in-solution source generator referenced as an analyzer'
    Assert = {
        param($repo)
        if ((Get-Content -Raw (Join-Path $repo 'src/App/Greeter.cs')) -match 'static public') { 'the code using the generated class was not formatted' }
    }
    Create = {
        param($root)
        New-Project $root 'src/Generator/Generator.csproj' -Frameworks 'netstandard2.0' -Properties ([ordered]@{ IsRoslynComponent = 'true'; EnforceExtendedAnalyzerRules = 'true'; LangVersion = 'latest' }) -Packages ([ordered]@{ 'Microsoft.CodeAnalysis.CSharp' = '4.8.0' }) -Extra @'
  <ItemGroup>
    <PackageReference Update="Microsoft.CodeAnalysis.CSharp" PrivateAssets="all" />
  </ItemGroup>
'@
        Set-Text $root 'src/Generator/HelloGenerator.cs' @'
using Microsoft.CodeAnalysis;

namespace Generator;

[Generator]
public sealed class HelloGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context) { context.RegisterPostInitializationOutput(c => c.AddSource("Hello.g.cs", "namespace App { internal static class Hello { public const string Text = \"hello\"; } }")); }
}
'@
        New-Project $root 'src/App/App.csproj' -Extra @'
  <ItemGroup>
    <ProjectReference Include="../Generator/Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  </ItemGroup>
'@
        Set-Text $root 'src/App/Greeter.cs' @'
namespace App;

public class Greeter
{
    static public string Greet() { return Hello.Text; }

    public string Name = "";
}
'@
        New-TestProject $root 'tests/App.Tests/App.Tests.csproj' -References '../../src/App/App.csproj'
        Set-Text $root 'tests/App.Tests/GreeterTests.cs' @'
using Xunit;

namespace App.Tests;

public class GreeterTests
{
    [Fact]
    public void Greets() { Assert.Equal("hello", App.Greeter.Greet()); }
}
'@
        New-Solution $root 'Fixture.sln' 'src/Generator/Generator.csproj', 'src/App/App.csproj', 'tests/App.Tests/App.Tests.csproj'
    }
}
