# BRO1101: only between two blank lines
@{
    File = 'src/StyleBro.Analyzers/Readability/EmptyStatements.cs'
    Find = 'group.Key > 0 && group.Key + 1 < text.Lines.Count && IsBlankLine(text, text.Lines[group.Key - 1])'
    Replace = 'group.Key > 0 && group.Key + 1 < text.Lines.Count'
    Tests = 'EmptyStatementTests'
}
