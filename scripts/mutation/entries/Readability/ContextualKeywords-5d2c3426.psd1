# BRO1144 (C# 14 contextual keywords; the C# 14 'field' type check is covered by samples/Messy, Roslyn 4.8 can't parse it)
@{
    File = 'src/StyleBro.Analyzers/Readability/ContextualKeywords.cs'
    Find = 'return first == token;'
    Replace = 'return true;'
    Tests = 'ContextualKeywordTests'
}
