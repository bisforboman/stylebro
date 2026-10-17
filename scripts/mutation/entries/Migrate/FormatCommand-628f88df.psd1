# Trial 28: a failed build shows next to its findings
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var buildFailed = exitCode != 0 && (found.Count == 0 || errors.Count > 0 ||'
    Replace = 'var buildFailed = exitCode != 0 && (found.Count == 0 ||'
    Tests = 'MigrationTests.Verify_ShowsAFailedBuild_NextToItsFindings'
}
