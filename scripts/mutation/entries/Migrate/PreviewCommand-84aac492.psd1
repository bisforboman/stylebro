# Trial 16: CSharpier where the solution is, its using order, CPM below the props, packages.config in the preview
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '.Union(CentralProjects(file, files, scope), StringComparer.OrdinalIgnoreCase)'
    Replace = ''
    Tests = 'MigrationTests.AddPackage_WithCentralPackagesOnlyBelowTheProps'
}
