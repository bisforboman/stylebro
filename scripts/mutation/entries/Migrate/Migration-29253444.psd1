# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (setup.Raw?.TryGetValue("SST1149", out var isNull) == true && isNull >= Severity.Suggestion)'
    Replace = 'if (setup.Raw is not null)'
    Tests = 'MigrationTests.Migrate_StyleSharpsIsNull_IsBro1133WithPatternMatching'
}
