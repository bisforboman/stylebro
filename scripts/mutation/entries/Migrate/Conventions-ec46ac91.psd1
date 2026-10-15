# Usings are sorted per run between directives
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (run.Count > 0 && u.GetLeadingTrivia().Any(t => t.IsDirective))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Conventions_UsingsInsideIf_AreSortedOnTheirOwn'
}
