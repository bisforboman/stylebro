# Backlog 086: paying off src/ leaves src.Tests/ alone
@{
    File = 'src/StyleBro.Migrate/BaselineCommand.PayOff.cs'
    Find = 'file.StartsWith(s + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)'
    Replace = 'file.StartsWith(s, StringComparison.OrdinalIgnoreCase)'
    Tests = 'MigrationTests.PayOff_TakesOutTheFolderEntries_FormatsTheirFiles'
}
