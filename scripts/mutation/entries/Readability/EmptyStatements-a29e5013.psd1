# BRO1101: a removed ';' line between two blank lines takes one along
@{
    File = 'src/StyleBro.Analyzers/Readability/EmptyStatements.cs'
    Find = '&& IsBlankLine(text, text.Lines[group.Key + 1])'
    Replace = ''
    Tests = 'EmptyStatementTests'
}
