# BRO1139 (else if)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'
    Find = 'return shift < 0 ? null :'
    Replace = 'return false ? null :'
    Tests = 'ElseIfTests'
}
