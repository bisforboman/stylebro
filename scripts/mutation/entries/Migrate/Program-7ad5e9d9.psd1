# Trial 13 (2026-10-13): conditional StyleCop references, leftover StyleCop settings, report fixes
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = '(Migration.IsGeneratedOrVendored(relative, text) || '
    Replace = '('
    Tests = 'MigrationTests.Suppressions_SkipGeneratedCode_'
}
