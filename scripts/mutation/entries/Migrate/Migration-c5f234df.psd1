# init's report: short verdicts, plurals, the defaults the code follows, wrapped summary (2026-10-10)
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '? Nearest(Path.GetDirectoryName(directory)) : candidate;'
    Replace = '? candidate : candidate;'
    Tests = 'MigrationTests'
}
