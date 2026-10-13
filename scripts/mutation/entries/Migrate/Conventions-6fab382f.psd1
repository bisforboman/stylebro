# Trial 16: CSharpier where the solution is, its using order, CPM below the props, packages.config in the preview
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '.Where(l => l.Target?.Identifier.Text == attributeList.Target?.Identifier.Text)'
    Replace = ''
    Tests = 'MigrationTests.Conventions_CountDeclarationsWithSeveralAttributes'
}
