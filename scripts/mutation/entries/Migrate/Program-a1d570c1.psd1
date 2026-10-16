# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = ' && !Migration.StyleSharpReplaced.Contains(r.Id))'
    Replace = ')'
    Tests = 'MigrationTests.Migrate_StyleSharpsIsNull_IsBro1133WithPatternMatching'
}
