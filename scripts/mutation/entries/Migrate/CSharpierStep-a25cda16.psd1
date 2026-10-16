# With lf, every CR is another ending.
@{
    File = 'src/StyleBro.Migrate/CSharpierStep.cs'
    Find = '(!crlf || i + 1 >= text.Length'
    Replace = '(i + 1 >= text.Length'
    Tests = 'MigrationTests.CSharpier_TakesTheFilesWhoseLineEndingsArentEndOfLines'
}
