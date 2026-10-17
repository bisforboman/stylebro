# Trial 28: code a skipped framework compiles is not verified
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '|| l.StartsWith(PartlyIncompleteMarker, StringComparison.Ordinal))'
    Replace = ')'
    Tests = 'MigrationTests.Verify_IsNotVerified_WhenAFrameworksCodeWentUnchecked'
}
