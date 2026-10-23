# No builds, no proof (trial 46)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (!built || buildLine is not null || '
    Replace = 'if (buildLine is not null || '
    Tests = 'MigrationTests.CopyOffVerdict'
}
