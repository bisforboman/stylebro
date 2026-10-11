# BRO1139 (else if)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'
    Find = '|| gaps.Any('
    Replace = '|| gaps.All('
    Tests = 'ElseIfTests'
}
