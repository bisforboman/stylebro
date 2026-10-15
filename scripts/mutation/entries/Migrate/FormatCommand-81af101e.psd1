# Mask recovery leaves a file whose swapped characters changed
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (found?.Count == count)'
    Replace = 'if (found is not null)'
    Tests = 'MigrationTests.Format_PutsBackWhatAKilledRun'
}
