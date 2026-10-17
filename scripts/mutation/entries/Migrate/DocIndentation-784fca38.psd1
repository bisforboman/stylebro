# Trial 27: a line at another column is the whitespace pass's
@{
    File = 'src/StyleBro.Migrate/DocIndentation.cs'
    Find = '(isComment || isToken) && Column(own, tabWidth) == column && '
    Replace = '(isComment || isToken) && '
    Tests = 'MigrationTests.DocIndentation_RewritesTheLinesTheWhitespacePassLeaves'
}
