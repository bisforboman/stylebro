# Trial 16: CSharpier where the solution is, its using order, CPM below the props, packages.config in the preview
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (scope.Length == 0 || cs.All('
    Replace = 'if (true || cs.All('
    Tests = 'MigrationTests.CSharpier_FormattingOneFolder'
}
