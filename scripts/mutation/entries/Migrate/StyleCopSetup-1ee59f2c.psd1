# trial 48: a package global config is a layer under the repository's own
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'packageInEffect.ForEach(config => Overlay(result, config));'
    Replace = ''
    Tests = 'MigrationTests.Read_CountsTheGlobalConfigAPackageAdds'
}
