# Trial 25: --files puts a new baseline where a whole run puts it
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return found ?? Path.Combine(Path.GetFullPath(newAt ?? root), '
    Replace = 'return found ?? Path.Combine(Path.GetFullPath(root), '
    Tests = 'MigrationTests.Format_PutsANewBaselineForFiles_WhereAWholeRunPutsIt'
}
