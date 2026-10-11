# init's report: short verdicts, plurals, the defaults the code follows, wrapped summary (2026-10-10)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'found = winner < 0 ? Plural(total, convention.Unit) : found;'
    Replace = 'found = found;'
    Tests = 'MigrationTests'
}
