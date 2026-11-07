# Backlog 083: the run's own headers stay out of the record's notes.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '|| (IsHeaderLine(line) && !RunHeaders.Any(h => line.StartsWith(h, StringComparison.Ordinal)));'
    Replace = '|| IsHeaderLine(line);'
    Tests = 'MigrationTests.IsRecordedNote_KeepsTheRepositorysNotes_NotTheRunsHeaders'
}
