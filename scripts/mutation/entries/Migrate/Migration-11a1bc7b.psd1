# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'l.Include.IndexOfAny([''*'', ''?'', ''$'', ''%'', ''@'']) >= 0'
    Replace = 'false'
    Tests = 'MigrationTests.OwnCSharpFiles_BelowNoProject_CountOnlyWhenNothingCouldCompileThem'
}
