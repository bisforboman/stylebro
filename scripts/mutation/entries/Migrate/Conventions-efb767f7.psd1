# Trial 14 (Kavita): only doc comments BRO1513 wants a blank line above count (not right after '{')
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '&& StyleBro.Analyzers.Layout.DocumentationBlankLines.WantsBlankLineAbove(text.Lines[above + 1], text))'
    Replace = ')'
    Tests = 'MigrationTests.Separation_CompactCodeTurnsBro1505AndBro1513Off'
}
