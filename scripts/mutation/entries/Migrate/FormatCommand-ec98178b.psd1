# Trial 27: a failed build without findings of its own is not clean
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (exitCode != 0 && found.Count == 0)'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Verify_IsNotClean_WhenTheBuildFailsWithoutFindingsOfItsOwn'
}
