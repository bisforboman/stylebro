# Trial 22: a file converted to the other ending without end_of_line gets its own back (d4b)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'changed.Where(e => e.Now is null || !keep.Contains(e.File))'
    Replace = 'changed.Where(e => e.Now is null)'
    Tests = 'MigrationTests.Format_GivesFilesTheirEndingBack'
}
