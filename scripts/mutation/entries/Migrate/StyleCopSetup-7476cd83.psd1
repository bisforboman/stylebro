# Trial 13 (2026-10-13): conditional StyleCop references, leftover StyleCop settings, report fixes
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'if (id is not null && (id.StartsWith("SA", StringComparison.OrdinalIgnoreCase)'
    Replace = 'if (id is not null && (id.StartsWith("SA", StringComparison.Ordinal)'
    Tests = 'MigrationTests.LowerCaseStyleCopIds_'
}
