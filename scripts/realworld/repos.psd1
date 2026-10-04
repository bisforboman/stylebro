# Public repositories the real-world check runs StyleBro on, pinned to the commits the results in CLAUDE.md were
# measured on. Tests: 'dotnet' (dotnet test), 'mtp' (Microsoft Testing Platform: run the test executables), or $null
# (no tests: OpenTelemetry's depend on the machine, see CLAUDE.md). KnownFailures: tests the fixes are known to break, each
# a documented limit (they don't fail the check). MaxRuns: 'dotnet format' runs until a run changes nothing, when a repo
# needs more than 2 (one that changes, one clean) for a documented reason.
@{
    FFMpegCore    = @{ Url = 'https://github.com/rosenbjerg/FFMpegCore'; Commit = '1fd8c88c45bc683c0c2cd0fe7e9a2f74642e9646'; Solution = 'FFMpegCore.sln'; Tests = 'dotnet' }
    Polly         = @{ Url = 'https://github.com/App-vNext/Polly'; Commit = '0275bc22a5a8defb77415313112454a558d0f43a'; Solution = 'Polly.slnx'; Tests = 'mtp' }
    OpenTelemetry = @{ Url = 'https://github.com/open-telemetry/opentelemetry-dotnet'; Commit = 'ea1e3f36105ecfeac0595b7807ad73983bab1f75'; Solution = 'OpenTelemetry.slnx'; Tests = $null }
    Newtonsoft    = @{ Url = 'https://github.com/JamesNK/Newtonsoft.Json'; Commit = '52fa3aef1f2cadcd3a3f874251eddc98d3efbbaa'; Solution = 'Src/Newtonsoft.Json.slnx'; Tests = 'dotnet'
        # BRO1001 sorts PrivateMembersClass's fields; the test serializes them by reflection (BindingFlags.NonPublic), with no
        # attribute that would show the order matters (docs/rules/BRO1001.md, Not reported).
        KnownFailures = @('Newtonsoft.Json.Tests.Serialization.CamelCasePropertyNamesContractResolverTests.MemberSearchFlags') }
    Serilog       = @{ Url = 'https://github.com/serilog/serilog'; Commit = 'bebc7719004f76187ae72e64ce138ec2540f2070'; Solution = 'Serilog.sln'; Tests = 'dotnet' }
    # An application (a media server) that uses StyleCop.Analyzers.
    Jellyfin      = @{ Url = 'https://github.com/jellyfin/jellyfin'; Commit = '305a96471509f11de4d3a93e1ea268d4fd2af76c'; Solution = 'Jellyfin.sln'; Tests = 'dotnet' }
    FluentValidation = @{ Url = 'https://github.com/FluentValidation/FluentValidation'; Commit = 'fa3c160b17796ff67d6aa5ae6c4b05b471f1a791'; Solution = 'FluentValidation.sln'; Tests = 'dotnet' }
    # Seven target frameworks (net462 to net9.0). Three tests fail on the untouched code too; only new failures count.
    CsvHelper     = @{ Url = 'https://github.com/JoshClose/CsvHelper'; Commit = '33970e5183383bdac1fbce3b3fbcdf46b318ca52'; Solution = 'CsvHelper.sln'; Tests = 'dotnet' }
}
