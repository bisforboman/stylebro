# Trial 16: CSharpier where the solution is, its using order, CPM below the props, packages.config in the preview
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'packagesConfig.Count > 0 && packagesConfig.Count == StyleCopSetup.EnumerateFiles(pathFolder)'
    Replace = 'packagesConfig.Count > 0 && 0 < StyleCopSetup.EnumerateFiles(pathFolder)'
    Tests = 'MigrationTests.Preview_LeavesPackagesConfigProjectsOut'
}
