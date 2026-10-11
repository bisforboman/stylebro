# BRO1148 (HasValue -> null check)
@{
    File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'
    Find = '|| (pattern && ((CSharpParseOptions)node.SyntaxTree.Options).LanguageVersion'
    Replace = '|| (false && ((CSharpParseOptions)node.SyntaxTree.Options).LanguageVersion'
    Tests = 'HasValueTests'
}
