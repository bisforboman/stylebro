# The StyleCop migration detects CSharpier like init.
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (csharpierScopes is [(var csharpier, "")])'
    Replace = 'if (csharpierScopes is [(var csharpier, "")] && false)'
    Tests = 'MigrationTests.Migration_WithCSharpier'
}
