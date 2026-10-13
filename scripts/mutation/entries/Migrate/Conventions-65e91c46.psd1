# Trial 16: CSharpier where the solution is, its using order, CPM below the props, packages.config in the preview
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '.SelectMany(f => Ancestors(Path.GetDirectoryName(f)!))'
    Replace = '.Where(_ => false)'
    Tests = 'MigrationTests.CSharpier_IsFoundWhereTheSolutionIs'
}
