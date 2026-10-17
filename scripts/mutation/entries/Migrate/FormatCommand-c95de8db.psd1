# One project: its default configuration.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '{(names.Count == 1 ? "it takes" : "they take")}'
    Replace = 'they take'
    Tests = 'MigrationTests.Verify_BuildsTheConfigurationsFormatSkipsForAGeneratorProject'
}
