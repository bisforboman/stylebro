# One project: its default configuration.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '{(names.Count == 1 ? "its" : "their")} default configuration only'
    Replace = 'their default configuration only'
    Tests = 'MigrationTests.Verify_BuildsTheConfigurationsFormatSkipsForAGeneratorProject'
}
