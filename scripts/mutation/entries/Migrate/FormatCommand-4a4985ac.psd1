# format: projects outside the solution only in the default configuration's runs
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Where(i => runs[i].Configuration.Length == 0 && runs[i].Projects'
    Replace = '.Where(i => runs[i].Projects'
    Tests = 'MigrationTests.Format_GivesAProjectOutsideTheSolution'
}
