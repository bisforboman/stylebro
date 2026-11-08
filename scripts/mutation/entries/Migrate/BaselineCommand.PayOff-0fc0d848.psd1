# Backlog 086: entries for files that are gone are dropped, not formatted
@{
    File = 'src/StyleBro.Migrate/BaselineCommand.PayOff.cs'
    Find = '.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).Order('
    Replace = '.Distinct(StringComparer.OrdinalIgnoreCase).Order('
    Tests = 'MigrationTests.PayOff_NothingToDo_AndUsageErrors'
}
