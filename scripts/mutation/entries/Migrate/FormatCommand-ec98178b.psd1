# Trial 27: a failed build without findings of its own is not clean
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (buildFailed)'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Verify_IsNotClean_WhenTheBuildFailsWithoutFindingsOfItsOwn'
}
