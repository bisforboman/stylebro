# BRO1524 (split conditional expressions)
@{
    File = 'src/StyleBro.Analyzers/Layout/ConditionalLayout.cs'
    Find = 'if (gaps.SelectMany(g => g).Any('
    Replace = 'if (false && gaps.SelectMany(g => g).Any('
    Tests = 'ConditionalLayoutTests'
}
