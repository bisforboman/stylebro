# Trial 28: no "no findings." right before "failed"
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (found.Count > 0 || !buildFailed)'
    Replace = 'if (true)'
    Tests = 'MigrationTests.Verify_IsNotClean_WhenTheBuildFailsWithoutFindingsOfItsOwn'
}
