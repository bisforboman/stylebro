# Backlog 083: the workspace warnings' note is recorded.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'line.StartsWith(WorkspaceWarningsNote, StringComparison.Ordinal)
        ||'
    Replace = 'false ||'
    Tests = 'MigrationTests.IsRecordedNote_KeepsTheRepositorysNotes_NotTheRunsHeaders'
}
