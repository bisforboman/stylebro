# init's report: short verdicts, plurals, the defaults the code follows, wrapped summary (2026-10-10)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '(count == 1 ? string.Empty : "s")'
    Replace = '"s"'
    Tests = 'MigrationTests'
}
