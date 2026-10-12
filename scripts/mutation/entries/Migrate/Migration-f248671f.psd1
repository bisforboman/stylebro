# The StyleCop migration detects CSharpier like init.
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (Conventions.CSharpier(root) is { } csharpier)'
    Replace = 'if (Conventions.CSharpier(root) is { } csharpier && false)'
    Tests = 'FullyQualifiedName~MigrationTests.Migration_WithCSharpier'
}
