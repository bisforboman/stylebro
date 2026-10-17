# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'return owner is null ? !outsideCounts &&'
    Replace = 'return owner is null ? false &&'
    Tests = 'MigrationTests.OwnCSharpFiles_BelowNoProject_CountOnlyWhenNothingCouldCompileThem'
}
