# Trial 25: --files puts a new baseline where a whole run puts it
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var at = Path.GetFullPath(newAt ?? NewBaselineFolder(root));'
    Replace = 'var at = Path.GetFullPath(NewBaselineFolder(root));'
    Tests = 'MigrationTests.Format_PutsANewBaselineForFiles_WhereAWholeRunPutsIt'
}
