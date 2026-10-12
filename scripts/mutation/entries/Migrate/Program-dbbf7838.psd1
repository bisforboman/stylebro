# Trial 13 (2026-10-13): conditional StyleCop references, leftover StyleCop settings, report fixes
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'r.Value.Where(id => !Suppressions.Ineffective.Contains(id))'
    Replace = 'r.Value'
    Tests = 'MigrationTests.Suppressions_SkipGeneratedCode_'
}
