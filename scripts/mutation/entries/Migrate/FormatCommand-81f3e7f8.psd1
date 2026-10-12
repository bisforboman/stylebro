# Trial 10, tool (2026-10-12): before the first restore the assets file comes from MSBuildProjectExtensionsPath
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = ': hasProperties && properties.TryGetProperty("MSBuildProjectExtensionsPath"'
    Replace = ': false && properties.TryGetProperty("MSBuildProjectExtensionsPath"'
    Tests = 'MigrationTests.Format_StyleBroLoads_'
}
