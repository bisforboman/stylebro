# Format host (phase 1, 2026-10-22): the SDK's load context must not hand out the tool's own StyleBro.Analyzers (Roslyn 4.8), or no analyzer loads
@{
    File = 'src/StyleBro.Migrate/FormatHost.cs'
    Find = '!File.Exists(Path.Combine(formatFolder, name + ".dll")) && File.Exists(Path.Combine(toolFolder, name + ".dll"))'
    Replace = 'false'
    Tests = 'FormatHostTests.TheSdkContext_RefusesTheToolsOwnAssemblies'
}
