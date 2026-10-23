# trial 48: no restore warning when only StyleCop is referenced
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'if (Chain(project, root).Any(f => OtherPackage.IsMatch('
    Replace = 'if (true || Chain(project, root).Any(f => OtherPackage.IsMatch('
    Tests = 'MigrationTests.Read_CountsTheGlobalConfigAPackageAdds'
}
