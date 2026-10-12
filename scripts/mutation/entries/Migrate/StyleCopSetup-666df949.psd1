# migrate (trial 11, Autofac): after the swap the recorded folders without StyleCop still don't count
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'if (recorded is not null && !projects.Uses.Values.Contains(true)'
    Replace = 'if (false'
    Tests = 'MigrationTests.SecondWrite_AfterThePackageSwap'
}
