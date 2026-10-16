# Trial 24 audit: SST1106 accepts a semicolon after a type
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '            lines.Add($"{StyleBro.Analyzers.Readability.EmptyStatementAnalyzer.AllowAfterClosingBraceKey} = true");'
    Replace = ''
    Tests = 'MigrationTests.StyleSharp_WritesItsOwnMeaning_WhereItsRuleDiffersFromStyleCops'
}
