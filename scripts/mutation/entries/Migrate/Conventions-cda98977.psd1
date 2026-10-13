# Trial 16: CSharpier where the solution is, its using order, CPM below the props, packages.config in the preview
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '&& decided.TryGetValue(SortUsingsKey, out var reason))'
    Replace = '&& decided.TryGetValue("-", out var reason))'
    Tests = 'MigrationTests.Init_WithCSharpier_WritesNoUsingSortKey'
}
