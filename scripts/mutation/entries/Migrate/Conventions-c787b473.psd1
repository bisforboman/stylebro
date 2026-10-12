# Without a .csharpierrc, CSharpier reads max_line_length.
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'settings.TryGetValue("max_line_length", out var value)'
    Replace = 'settings.TryGetValue("max_line_length_x", out var value)'
    Tests = 'FullyQualifiedName~MigrationTests.CSharpierPrintWidth'
}
