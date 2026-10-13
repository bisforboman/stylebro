# Trial 16: CSharpier where the solution is, its using order, CPM below the props, packages.config in the preview
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '.Where(m => !Conditional(m.Value) && '
    Replace = '.Where(m => '
    Tests = 'MigrationTests.AddPackageVersion_SkipsConditionalItemGroups'
}
