# Only StyleCop rules that nothing but CSharpier rules replaced are CSharpier's (SA1514 stays StyleBro's).
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'r.Value.All(Conventions.CSharpierRules.Contains)'
    Replace = 'r.Value.Any(Conventions.CSharpierRules.Contains)'
    Tests = 'FullyQualifiedName~MigrationTests.Migration_WithCSharpier'
}
