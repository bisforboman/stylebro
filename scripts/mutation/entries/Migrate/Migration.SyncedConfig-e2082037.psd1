# A copy the builds showed is off isnt named (trial 46)
@{
    File = 'src/StyleBro.Migrate/Migration.SyncedConfig.cs'
    Find = ' && !CopyIsOff(root, copy) ? copy : null;'
    Replace = ' ? copy : null;'
    Tests = 'MigrationTests.PackageCopy_IsGone'
}
