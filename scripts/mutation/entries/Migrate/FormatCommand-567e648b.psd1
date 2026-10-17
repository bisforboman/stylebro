# Trial 25: a .slnx without BuildType elements builds Debug and Release
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '(types.Count > 0 ? types : ["Debug", "Release"])'
    Replace = '(types)'
    Tests = 'MigrationTests.Format_ReadsTheConfigurationsASolutionBuilds'
}
