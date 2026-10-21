# Trial 40: a folder file adds to the root's (no root = true)
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'Existing = existing ?? string.Empty,'
    Replace = 'Existing = existing,'
    Tests = 'StyleBro.Tests.MigrationTests.Init_WritesItsBlockIntoTheProjectFolders_WhenTheBuildCopiesTheRootEditorConfigFromASubmodule'
}
