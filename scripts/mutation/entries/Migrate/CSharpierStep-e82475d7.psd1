# Only where end_of_line says lf or crlf.
@{
    File = 'src/StyleBro.Migrate/CSharpierStep.cs'
    Find = '&& value.Trim().ToLowerInvariant() is "lf" or "crlf"'
    Replace = ''
    Tests = 'MigrationTests.CSharpier_TakesTheFilesWhoseLineEndingsArentEndOfLines'
}
