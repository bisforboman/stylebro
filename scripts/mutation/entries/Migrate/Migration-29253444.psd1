# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (StyleSharpSeverity(setup, "SST1149") >= Severity.Suggestion)'
    Replace = 'if (setup.StyleSharp is not null)'
    Tests = 'MigrationTests.Migrate_StyleSharpsIsNull_IsBro1133WithPatternMatching'
}
