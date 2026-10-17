# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'var outsideCounts = projects.Count == 0 ||'
    Replace = 'var outsideCounts ='
    Tests = 'MigrationTests.OwnCSharpFiles_BelowNoProject_CountOnlyWhenNothingCouldCompileThem'
}
