# BRO1135 (upper-case integer literal suffixes)
@{
    File = 'src/StyleBro.Analyzers/Readability/LiteralSuffixes.cs'
    Find = 'if (scope == SuffixScope.LOnly)'
    Replace = 'if (false)'
    Tests = 'LiteralSuffixCaseTests'
}
