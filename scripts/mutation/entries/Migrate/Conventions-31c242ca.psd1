# init/migrate (trial 32, RazorLight): a file whose string literal spans lines keeps its ending
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '.DescendantTokens().Any(t => t.Text.Contains(''\n''))'
    Replace = '.DescendantTokens().Any(t => false)'
    Tests = 'MigrationTests.LineEndings_AFileWhoseStringLiteralSpansLines'
}
