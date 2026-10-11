# init's report: short verdicts, plurals, the defaults the code follows, wrapped summary (2026-10-10)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '(!parenthesized || level > outer || (level == outer && operand == binary.Left))'
    Replace = 'true'
    Tests = 'MigrationTests'
}
