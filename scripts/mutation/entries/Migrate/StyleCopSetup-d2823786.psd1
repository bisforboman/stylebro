# StyleCop referenced for one target framework of a multi-targeted project counts
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'return frameworks.Length > 1 && frameworks.AsParallel()'
    Replace = 'return frameworks.Length > 9 && frameworks.AsParallel()'
    Tests = 'MigrationTests.UsesStyleCop_ChecksEveryTargetFramework'
}
