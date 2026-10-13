# Trial 16: CSharpier where the solution is, its using order, CPM below the props, packages.config in the preview
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'var covers = scope.Length == 0 ||'
    Replace = 'var covers = true ||'
    Tests = 'MigrationTests.CSharpier_FormattingOneFolder'
}
