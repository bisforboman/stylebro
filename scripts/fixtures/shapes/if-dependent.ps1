# Trials 43 and 57: code whose shape depends on '#if'.
# - DapperAOT (trial 57): BRO1404 added 'internal' to '#if !SYMBOL' / 'file' / '#endif' / 'static class X', which a project
#   compiling the same file with the symbol defined then rejected (CS9052-like). Modifiers that depend on #if are skipped.
# - NSwag (trial 43): a type whose header has '#if' branches, in a file two projects compile, flipped a cref every run.
# - Members that exist in one framework only.
# Two projects compile Shared/Helpers.cs, one with HELPERS_INTERNAL defined.
@{
    Description = '#if-dependent modifiers, headers and members in a file two projects compile'
    Create = {
        param($root)
        Set-Text $root 'src/Shared/Helpers.cs' @'
namespace Shared
{
#if !HELPERS_INTERNAL
    public
#endif
    static class Helpers
    {
        static public int Twice(int x) { return x * 2; }

#if NET10_0_OR_GREATER
        public static string Framework => "net10.0";
#else
        public static string Framework => "net8.0";
#endif
    }

    /// <summary>
    /// Settings, see <see cref="Settings"/>.
    /// </summary>
#if HELPERS_INTERNAL
    internal class Settings
#else
    public class Settings
#endif
    {
        public string Name = "";
    }
}
'@
        New-Project $root 'src/Public/Public.csproj' -Frameworks 'net8.0;net10.0' -Extra @'
  <ItemGroup>
    <Compile Include="../Shared/Helpers.cs" Link="Helpers.cs" />
  </ItemGroup>
'@
        New-Project $root 'src/Internal/Internal.csproj' -Properties ([ordered]@{ DefineConstants = '$(DefineConstants);HELPERS_INTERNAL' }) -Extra @'
  <ItemGroup>
    <Compile Include="../Shared/Helpers.cs" Link="Helpers.cs" />
  </ItemGroup>
'@
        Set-Text $root 'src/Internal/Uses.cs' "namespace Internal;`n`npublic static class Uses`n{`n    public static int Four() => Shared.Helpers.Twice(2);`n}"
        New-TestProject $root 'tests/Public.Tests/Public.Tests.csproj' -Frameworks 'net8.0;net10.0' -References '../../src/Public/Public.csproj', '../../src/Internal/Internal.csproj' -Extra @'
  <ItemGroup>
    <ProjectReference Update="../../src/Internal/Internal.csproj" Aliases="Internal" />
  </ItemGroup>
'@
        Set-Text $root 'tests/Public.Tests/HelpersTests.cs' @'
using Xunit;

namespace Public.Tests;

public class HelpersTests
{
    [Fact]
    public void Twice() { Assert.Equal(4, Shared.Helpers.Twice(2)); }

    [Fact]
    public void Framework()
    {
#if NET10_0_OR_GREATER
        Assert.Equal("net10.0", Shared.Helpers.Framework);
#else
        Assert.Equal("net8.0", Shared.Helpers.Framework);
#endif
    }
}
'@
        New-Solution $root 'Fixture.sln' 'src/Public/Public.csproj', 'src/Internal/Internal.csproj', 'tests/Public.Tests/Public.Tests.csproj'
    }
}
