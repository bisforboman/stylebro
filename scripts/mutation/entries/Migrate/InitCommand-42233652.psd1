# A path section's key is repeated only where the block says otherwise
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '&& !value.Equals(blockValue, StringComparison.OrdinalIgnoreCase))'
    Replace = ')'
    Tests = 'MigrationTests.Init_RepeatsTheFilesOwnPathSections'
}
