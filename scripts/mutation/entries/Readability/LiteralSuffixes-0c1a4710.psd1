# BRO1135 (upper-case integer literal suffixes)
@{
    File = 'src/StyleBro.Analyzers/Readability/LiteralSuffixes.cs'
    Find = 'return suffix.Any(char.IsLower) ?'
    Replace = 'return true ?'
    Tests = 'LiteralSuffixCaseTests'
}
