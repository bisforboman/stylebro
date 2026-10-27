# Trial 56 (FusionCache, #306): net10.0 projects reach a core library (which has net10.0) through a netstandard2.0-only
# plugin as well as directly: one framework's run sees the core through two paths. Trial 51 (Pipelines.Sockets.Unofficial):
# a run kept a project for a framework it doesn't target. Every framework's code must be checked, once.
@{
    Description = 'a project lacking one of the solution''s frameworks, reached through two paths'
    Create = {
        param($root)
        New-Project $root 'src/Core/Core.csproj' -Frameworks 'netstandard2.0;net10.0' -Properties ([ordered]@{ LangVersion = 'latest' })
        Set-Text $root 'src/Core/Cache.cs' @'
namespace Core;

public class Cache
{
#if NET10_0_OR_GREATER
    static public string Framework() { return "net10.0"; }
#else
    static public string Framework() { return "netstandard2.0"; }
#endif

    public string Name = "";
}
'@
        New-Project $root 'src/Plugin/Plugin.csproj' -Frameworks 'netstandard2.0' -Properties ([ordered]@{ LangVersion = 'latest' }) -References '../Core/Core.csproj'
        Set-Messy $root 'src/Plugin/Plugin.cs' -Namespace 'Plugin'
        New-Project $root 'src/App/App.csproj' -Frameworks 'net8.0;net10.0' -References '../Plugin/Plugin.csproj', '../Core/Core.csproj'
        Set-Messy $root 'src/App/Host.cs' -Namespace 'App'
        New-TestProject $root 'tests/App.Tests/App.Tests.csproj' -Frameworks 'net8.0;net10.0' -References '../../src/App/App.csproj'
        Set-Text $root 'tests/App.Tests/CacheTests.cs' @'
using Xunit;

namespace App.Tests;

public class CacheTests
{
    [Fact]
    public void Framework()
    {
#if NET10_0_OR_GREATER
        Assert.Equal("net10.0", Core.Cache.Framework());
#else
        Assert.Equal("netstandard2.0", Core.Cache.Framework());
#endif
    }
}
'@
        New-Solution $root 'Fixture.sln' 'src/Core/Core.csproj', 'src/Plugin/Plugin.csproj', 'src/App/App.csproj', 'tests/App.Tests/App.Tests.csproj'
    }
}
