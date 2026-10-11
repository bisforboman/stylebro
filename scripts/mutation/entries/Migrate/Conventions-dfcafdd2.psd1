# init's report: short verdicts, plurals, the defaults the code follows, wrapped summary (2026-10-10)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'while (line.Length > Width)'
    Replace = 'while (false)'
    Tests = 'MigrationTests'
}
