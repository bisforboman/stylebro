# trial 48: the repository's own global configs are read once
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'result.AddRange(found.Where(f => !InRepository(root, f) && '
    Replace = 'result.AddRange(found.Where(f => '
    Tests = 'MigrationTests.Read_CountsTheGlobalConfigAPackageAdds'
}
