# Trial 28: findings as errors are not a failed build
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var errors = lines.Where(m => m.Groups["kind"].Value == "error" && !ids.Contains(m.Groups["id"].Value))'
    Replace = 'var errors = lines.Where(m => m.Groups["kind"].Value == "error")'
    Tests = 'MigrationTests.Verify_ShowsAFailedBuild_NextToItsFindings'
}
