# BRO1133 (null check style)
@{
    File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'
    Find = '|| !IsLooseParent(isPattern)'
    Replace = ''
    Tests = 'NullCheckTests'
}
