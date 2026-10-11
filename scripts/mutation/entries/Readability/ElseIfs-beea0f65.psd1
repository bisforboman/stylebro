# BRO1139 (else if)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'
    Find = 'if (indent < shift)'
    Replace = 'if (false)'
    Tests = 'ElseIfTests'
}
