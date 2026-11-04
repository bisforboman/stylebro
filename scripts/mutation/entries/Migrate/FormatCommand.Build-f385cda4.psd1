# Only an MSB4018 file lock is retried (080).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = 'errors is [ "MSB4018" ] &&'
    Replace = 'errors.Count > 0 &&'
    Tests = 'FormatCoverageTests.IsOnlyFileLock'
}
