# Public repositories the real-world check runs StyleBro on, pinned to the commits the results in CLAUDE.md were
# measured on. Tests: 'dotnet' (dotnet test), 'mtp' (Microsoft Testing Platform: run the test executables), or $null
# (no tests: OpenTelemetry's depend on the machine, see CLAUDE.md). Env: environment variables for the repository's build. KnownFailures: tests the fixes are known to break, each
# a documented limit (they don't fail the check). TestFilter: a 'dotnet test --filter' that leaves out tests that depend on
# something other than the code (the network). MaxRuns: 'dotnet format' runs until a run changes nothing, when a repo
# needs more than 2 (one that changes, one clean) for a documented reason. Os: the runner, windows-latest unless set.
# ubuntu-26.04 only for a repo whose Linux run gives the same findings, files changed, runs and test results as on
# Windows (checked 2026-10-10): Serilog and MediatR drop their .NET Framework targets off Windows and lose findings there.
# The repos left on Windows also keep the CRLF checkouts (git on the Windows runners converts line endings).
# Formatter: 'csharpier' for a repository CSharpier formats: the init flow, then CSharpier, then both must be clean
# (Invoke-RealWorld.ps1). Its CSharpier version is the one in the repository's tool manifest.
# BroFormat: after the checks, 'bro init --write' + 'bro format' on the untouched code, which must settle (exit 2 fails;
# owner's choice 2026-10-23: Newtonsoft.Json only, the default engine).
# MaxFixSeconds: the fix-time limit, the 'dotnet format' runs together (until no changes). Over it, the job fails, naming
# the repo, the time and the limit (owner's choice 2026-10-24). The values are about 2x the median of 13 runs on each
# repo's own runner after the speed fixes of 2026-10-22 (PR #280): the nightlies of 2026-10-23 and -24 and the latest
# run of 11 PRs; the slowest of them was at most 1.27x its repo's median (runners vary about 2x between runs). Set them
# from the actual runners (Windows and Ubuntu differ), never from a local or cloud-container run.
# FixTimeWarnOnly = $true: over MaxFixSeconds only warns (a GitHub Actions warning and the job summary), for a repo whose
# fix time turns out too noisy to fail on.
# MaxBroFormatSeconds: the same for the BroFormat step ('bro init --write' + 'bro format'), warning only: only 2 runs had
# timed it (1162 s and 2401 s); make it a failure once nightlies show its median.
@{
    FFMpegCore    = @{ MaxFixSeconds = 90; Url = 'https://github.com/rosenbjerg/FFMpegCore'; Commit = '1fd8c88c45bc683c0c2cd0fe7e9a2f74642e9646'; Solution = 'FFMpegCore.sln'; Tests = 'dotnet' }
    Polly         = @{ MaxFixSeconds = 730; Url = 'https://github.com/App-vNext/Polly'; Commit = '0275bc22a5a8defb77415313112454a558d0f43a'; Solution = 'Polly.slnx'; Tests = 'mtp' }
    OpenTelemetry = @{ MaxFixSeconds = 800; Url = 'https://github.com/open-telemetry/opentelemetry-dotnet'; Commit = 'ea1e3f36105ecfeac0595b7807ad73983bab1f75'; Solution = 'OpenTelemetry.slnx'; Tests = $null }
    Newtonsoft    = @{ MaxFixSeconds = 1800; BroFormat = $true; MaxBroFormatSeconds = 3600; Url = 'https://github.com/JamesNK/Newtonsoft.Json'; Commit = '52fa3aef1f2cadcd3a3f874251eddc98d3efbbaa'; Solution = 'Src/Newtonsoft.Json.slnx'; Tests = 'dotnet'
        # BRO1001 sorts PrivateMembersClass's fields; the test serializes them by reflection (BindingFlags.NonPublic), with no
        # attribute that would show the order matters (docs/rules/BRO1001.md, Not reported).
        KnownFailures = @('Newtonsoft.Json.Tests.Serialization.CamelCasePropertyNamesContractResolverTests.MemberSearchFlags') }
    Serilog       = @{ MaxFixSeconds = 350; Url = 'https://github.com/serilog/serilog'; Commit = 'bebc7719004f76187ae72e64ce138ec2540f2070'; Solution = 'Serilog.sln'; Tests = 'dotnet' }
    # An application (a media server) that uses StyleCop.Analyzers.
    Jellyfin      = @{ MaxFixSeconds = 460; Url = 'https://github.com/jellyfin/jellyfin'; Os = 'ubuntu-26.04'; Commit = '305a96471509f11de4d3a93e1ea268d4fd2af76c'; Solution = 'Jellyfin.sln'; Tests = 'dotnet' }
    FluentValidation = @{ MaxFixSeconds = 240; Url = 'https://github.com/FluentValidation/FluentValidation'; Os = 'ubuntu-26.04'; Commit = 'fa3c160b17796ff67d6aa5ae6c4b05b471f1a791'; Solution = 'FluentValidation.sln'; Tests = 'dotnet' }
    # Seven target frameworks (net462 to net9.0). Three tests fail on the untouched code too; only new failures count.
    CsvHelper     = @{ MaxFixSeconds = 550; Url = 'https://github.com/JoshClose/CsvHelper'; Commit = '33970e5183383bdac1fbce3b3fbcdf46b318ca52'; Solution = 'CsvHelper.sln'; Tests = 'dotnet' }
    # An ASP.NET Core application: MVC controllers, Razor Pages, Blazor WebAssembly, minimal API endpoints, DI, EF Core.
    eShopOnWeb    = @{ MaxFixSeconds = 100; Url = 'https://github.com/dotnet-architecture/eShopOnWeb'; Os = 'ubuntu-26.04'; Commit = '4da8212117e87d808d4bbc7da6286fd2147ce606'; Solution = 'eShopOnWeb.sln'; Tests = 'dotnet' }
    # Generic interfaces and open generics resolved through DI.
    MediatR       = @{ MaxFixSeconds = 150; Url = 'https://github.com/jbogard/MediatR'; Commit = '916ef1b3d68ccdc96db8f914eaf1b32fc7db52c5'; Solution = 'MediatR.slnx'; Tests = 'dotnet' }
    # P/Invoke into libgit2 (native binaries come from a NuGet package), net472 + net8.0 with #if.
    # Its fetch, clone, push and remote-listing tests talk to remote git servers: they failed on CI after the fixes but
    # passed on the untouched code in the same job and locally, so they say nothing about the code. Left out.
    LibGit2Sharp  = @{ MaxFixSeconds = 460; Url = 'https://github.com/libgit2/libgit2sharp'; Commit = 'eaa698d078941fd5e3cc82b59b885cd35d8cc0f8'; Solution = 'LibGit2Sharp.sln'; Tests = 'dotnet'
        TestFilter = 'FullyQualifiedName!~FetchFixture&FullyQualifiedName!~NetworkFixture&FullyQualifiedName!~CloneFixture&FullyQualifiedName!~PushFixture&FullyQualifiedName!~CanFetchFromRemoteByName' }
    # A Roslyn source generator on the newest C#.
    Mapperly      = @{ MaxFixSeconds = 180; Url = 'https://github.com/riok/mapperly'; Os = 'ubuntu-26.04'; Commit = 'f87d48b12a6010a224ca26ad112fb48c07cd6d5d'; Solution = 'Riok.Mapperly.slnx'; Tests = 'dotnet' }
    # CSharpier repositories (Formatter): StyleBro's init flow and the repository's own CSharpier must settle. Mapperly again
    # (CSharpier 1.3.0), and Mediator, a source generator with snapshot tests (CSharpier 1.2.1).
    MapperlyCSharpier = @{ MaxFixSeconds = 450; Url = 'https://github.com/riok/mapperly'; Os = 'ubuntu-26.04'; Commit = 'f87d48b12a6010a224ca26ad112fb48c07cd6d5d'; Solution = 'Riok.Mapperly.slnx'; Tests = 'dotnet'; Formatter = 'csharpier' }
    Mediator      = @{ MaxFixSeconds = 830; Url = 'https://github.com/martinothamar/Mediator'; Os = 'ubuntu-26.04'; Commit = '8b87a0ed74747b7e4be907db220b930a50fbeb98'; Solution = 'Mediator.slnx'; Tests = 'dotnet'; Formatter = 'csharpier'
        Env = @{ NBGV_GitEngine = 'Disabled' } }
}
