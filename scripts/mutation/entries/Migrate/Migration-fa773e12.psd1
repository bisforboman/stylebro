# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '.Where(include => include.Contains("..", StringComparison.Ordinal) || include.Contains("$(", StringComparison.Ordinal))'
    Replace = '.Where(include => include.Contains("$(", StringComparison.Ordinal))'
    Tests = 'MigrationTests.OwnCSharpFiles_BelowNoProject_CountOnlyWhenNothingCouldCompileThem'
}
