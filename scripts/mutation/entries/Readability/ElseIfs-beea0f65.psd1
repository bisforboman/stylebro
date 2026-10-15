# BRO1139 (else if)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'
    Find = 'if (shift < 0 || indent < shift)'
    Replace = 'if (false)'
    Tests = 'ElseIfTests'
}
