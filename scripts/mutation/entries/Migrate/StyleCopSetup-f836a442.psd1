# Trial 13 (2026-10-13): conditional StyleCop references, leftover StyleCop settings, report fixes
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'if (value is { } evaluated)'
    Replace = 'if (value is { } evaluated && project.Length < 0)'
    Tests = 'MigrationTests.AConditionalStyleCopReference_'
}
