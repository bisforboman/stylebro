# BRO1135 (upper-case integer literal suffixes)
@{
    File = 'src/StyleBro.Analyzers/Readability/LiteralSuffixes.cs'
    Find = 'return suffix.IndexOf(''l'') >= 0 ?'
    Replace = 'return true ?'
    Tests = 'LiteralSuffixCaseTests'
}
