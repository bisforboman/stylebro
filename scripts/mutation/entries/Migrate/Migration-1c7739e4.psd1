# Trial 16: CSharpier where the solution is, its using order, CPM below the props, packages.config in the preview
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'lines.RemoveAll(l => KeyOf(l) is Conventions.SortUsingsKey or "dotnet_separate_import_directive_groups");'
    Replace = ''
    Tests = 'MigrationTests.Init_WithCSharpier_WritesNoUsingSortKey'
}
