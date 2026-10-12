# Trial 13 (2026-10-13): conditional StyleCop references, leftover StyleCop settings, report fixes
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'a.Attribute("Condition") is not null || '
    Replace = ''
    Tests = 'MigrationTests.ConditionalReference_'
}
