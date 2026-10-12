# format --verify-no-changes (trial 11): skips caused by F# projects stay warnings
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = ' && !lines.Any(l => l.StartsWith(LikelyCause, StringComparison.Ordinal)))'
    Replace = ')'
    Tests = 'MigrationTests.Verify_AProjectNoRunLoaded'
}
