# Trial 28: findings as warnings do not explain a failed build
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '|| !lines.Any(m => m.Groups["kind"].Value == "error" && ids.Contains(m.Groups["id"].Value)));'
    Replace = ');'
    Tests = 'MigrationTests.Verify_ShowsAFailedBuild_NextToItsFindings'
}
