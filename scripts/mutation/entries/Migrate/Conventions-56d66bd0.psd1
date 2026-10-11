# init's report: short verdicts, plurals, the defaults the code follows, wrapped summary (2026-10-10)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = ': written.Count == 0 ? $"{kept}, all already'
    Replace = ': false ? $"{kept}, all already'
    Tests = 'MigrationTests'
}
