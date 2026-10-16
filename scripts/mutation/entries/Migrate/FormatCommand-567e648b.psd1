# Trial 25: a .slnx without BuildType elements builds Debug and Release
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return types.Count > 0 ? types : ["Debug", "Release"];'
    Replace = 'return types;'
    Tests = 'MigrationTests.Format_ReadsTheConfigurationsASolutionBuilds'
}
