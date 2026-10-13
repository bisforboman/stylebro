# The StyleCop migration detects CSharpier like init.
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'else if (csharpierScope is var (csharpier, _))'
    Replace = 'else if (csharpierScope is var (csharpier, _) && false)'
    Tests = 'MigrationTests.Migration_WithCSharpier'
}
