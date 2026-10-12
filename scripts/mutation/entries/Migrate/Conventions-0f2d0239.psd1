# A sort key the repository sets against CSharpier's order is overridden.
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (Value(SortUsingsKey) is { } systemFirst && systemFirst != "true")'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Init_WithCSharpier'
}
