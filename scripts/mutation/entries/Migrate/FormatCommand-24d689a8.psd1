# init's report: short verdicts, plurals, the defaults the code follows, wrapped summary (2026-10-10)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '|| all.All(s.Value.Contains)'
    Replace = '|| true'
    Tests = 'MigrationTests'
}
