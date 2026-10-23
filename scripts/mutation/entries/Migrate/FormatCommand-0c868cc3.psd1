# Builds that report something prove nothing (trial 46)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (!built || buildLine is not null || '
    Replace = 'if (!built || '
    Tests = 'MigrationTests.CopyOffVerdict'
}
