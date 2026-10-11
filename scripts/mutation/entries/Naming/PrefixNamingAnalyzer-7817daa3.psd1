# Public API is renamed only with stylebro_rename_public_api = true
@{
    File = 'src/StyleBro.Analyzers/Naming/PrefixNamingAnalyzer.cs'
    Find = '&& PrefixNames.GetInterfaceName(symbol.Name) is { } newName
            && PublicApi.CanRename(symbol, context.Options.AnalyzerConfigOptionsProvider.GetOptions(node.SyntaxTree))'
    Replace = '&& PrefixNames.GetInterfaceName(symbol.Name) is { } newName
            && true'
    Tests = 'PrefixNamingTests.PublicApi_IsLeftAloneByDefault'
}
