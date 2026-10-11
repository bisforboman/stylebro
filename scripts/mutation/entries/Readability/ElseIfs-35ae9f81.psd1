# BRO1139 (else if)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'
    Find = 'if (lineEdits.Count > 0 && parts[0].If.DescendantTokens()'
    Replace = 'if (false && parts[0].If.DescendantTokens()'
    Tests = 'ElseIfTests'
}
