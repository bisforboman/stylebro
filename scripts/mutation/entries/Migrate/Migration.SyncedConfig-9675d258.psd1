# Trial 40: the block is read from the folder files
@{
    File = 'src/StyleBro.Migrate/Migration.SyncedConfig.cs'
    Find = '.FirstOrDefault(f => File.Exists(f) && File.ReadAllText(f).Contains(BeginMarker, StringComparison.Ordinal)) ?? path;'
    Replace = '.FirstOrDefault(f => false) ?? path;'
    Tests = 'StyleBro.Tests.MigrationTests.Migrate_WritesItsBlockIntoTheProjectFolders_WhenTheBuildCopiesTheRootEditorConfigFromASubmodule'
}
