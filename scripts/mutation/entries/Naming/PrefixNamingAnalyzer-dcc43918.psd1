# Public API is renamed only with stylebro_rename_public_api = true
@{
    File = 'src/StyleBro.Analyzers/Naming/PrefixNamingAnalyzer.cs'
    Find = '&& !InheritsName(symbol)
            && PublicApi.CanRename(symbol, context.Options.AnalyzerConfigOptionsProvider.GetOptions(node.SyntaxTree))'
    Replace = '&& !InheritsName(symbol)
            && true'
    Tests = 'PrefixNamingTests.PublicApi_IsLeftAloneByDefault'
}
