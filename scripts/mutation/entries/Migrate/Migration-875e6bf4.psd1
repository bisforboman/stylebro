# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'files.Any(f => f.EndsWith(".projitems", StringComparison.OrdinalIgnoreCase))'
    Replace = 'false'
    Tests = 'MigrationTests.OwnCSharpFiles_BelowNoProject_CountOnlyWhenNothingCouldCompileThem'
}
