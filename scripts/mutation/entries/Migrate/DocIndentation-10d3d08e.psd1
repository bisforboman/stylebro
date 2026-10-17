# Trial 27: a first line written otherwise is the whitespace pass's
@{
    File = 'src/StyleBro.Migrate/DocIndentation.cs'
    Find = ' || Indentation(Column(indentation, tabWidth), useTabs, tabWidth) != indentation)'
    Replace = ')'
    Tests = 'MigrationTests.DocIndentation_RewritesTheLinesTheWhitespacePassLeaves'
}
