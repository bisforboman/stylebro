# Trial 23: a clean run baselines its kept findings
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var baselined = settled ? BaselineKept(root, keptFindings, shown) : null;'
    Replace = 'string? baselined = null;'
    Tests = 'MigrationTests.Format_CleanRunWithKeptFindings_WritesTheBaseline'
}
