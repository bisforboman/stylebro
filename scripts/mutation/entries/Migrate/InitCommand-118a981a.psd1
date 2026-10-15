# Trial 22 (spot d2): init reads the root .globalconfig
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'var settings = folder.Length == 0 ? Migration.WithGlobalConfig(repository, ownSettings) : ownSettings;'
    Replace = 'var settings = ownSettings;'
    Tests = 'MigrationTests.Init_ReadsTheRootGlobalConfig'
}
