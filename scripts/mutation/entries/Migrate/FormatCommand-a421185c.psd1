# format --verify-no-changes (trial 11, Autofac): a project no run loaded isn't clean
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (lines.Any(l => l.StartsWith(IncompleteMarker, StringComparison.Ordinal))'
    Replace = 'if (false'
    Tests = 'MigrationTests.Verify_AProjectNoRunLoaded'
}
