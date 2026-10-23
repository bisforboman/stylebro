# Trial 47: a project generating docs only in some configurations gets their runs.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = ' || DocumentsByConfiguration(File.ReadAllText(f))'
    Replace = ''
    Tests = 'MigrationTests.Format_RunsTheConfigurationsThatGenerateDocsOfTheirOwn'
}
