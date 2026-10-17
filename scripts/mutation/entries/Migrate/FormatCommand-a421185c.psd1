# format --verify-no-changes (trial 11, Autofac): a project no run loaded isn't clean
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'l.StartsWith(IncompleteMarker, StringComparison.Ordinal) || l.StartsWith(PartlyIncompleteMarker'
    Replace = 'false || l.StartsWith(PartlyIncompleteMarker'
    Tests = 'MigrationTests.Verify_AProjectNoRunLoaded'
}
