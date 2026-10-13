# Trial 16: CSharpier where the solution is, its using order, CPM below the props, packages.config in the preview
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'lists.Any(l => l.Attributes.Count > 1)'
    Replace = 'true'
    Tests = 'MigrationTests.Conventions_CountDeclarationsWithSeveralAttributes'
}
