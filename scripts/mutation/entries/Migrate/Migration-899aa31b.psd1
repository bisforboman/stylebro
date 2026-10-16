# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = ': StyleCopSetup.StyleSharpRules.FirstOrDefault(r => r.Id == id).Default;'
    Replace = ': Severity.None;'
    Tests = 'MigrationTests.Migrate_StyleSharpsIsNull_IsBro1133WithPatternMatching'
}
