# Public repositories the real-world check runs StyleBro on, pinned to the commits the results in CLAUDE.md were
# measured on. Tests: 'dotnet' (dotnet test), 'mtp' (Microsoft Testing Platform: run the test executables), or $null
# (no tests: OpenTelemetry's depend on the machine, see CLAUDE.md).
@{
    FFMpegCore    = @{ Url = 'https://github.com/rosenbjerg/FFMpegCore'; Commit = '1fd8c88c45bc683c0c2cd0fe7e9a2f74642e9646'; Solution = 'FFMpegCore.sln'; Tests = 'dotnet' }
    Polly         = @{ Url = 'https://github.com/App-vNext/Polly'; Commit = '0275bc22a5a8defb77415313112454a558d0f43a'; Solution = 'Polly.slnx'; Tests = 'mtp' }
    OpenTelemetry = @{ Url = 'https://github.com/open-telemetry/opentelemetry-dotnet'; Commit = 'ea1e3f36105ecfeac0595b7807ad73983bab1f75'; Solution = 'OpenTelemetry.slnx'; Tests = $null }
    Newtonsoft    = @{ Url = 'https://github.com/JamesNK/Newtonsoft.Json'; Commit = '52fa3aef1f2cadcd3a3f874251eddc98d3efbbaa'; Solution = 'Src/Newtonsoft.Json.slnx'; Tests = 'dotnet' }
    Serilog       = @{ Url = 'https://github.com/serilog/serilog'; Commit = 'bebc7719004f76187ae72e64ce138ec2540f2070'; Solution = 'Serilog.sln'; Tests = 'dotnet' }
}
