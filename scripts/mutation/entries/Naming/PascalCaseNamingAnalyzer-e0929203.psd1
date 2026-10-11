# Public API is renamed only with stylebro_rename_public_api = true
@{
    File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'
    Find = '|| !PublicApi.CanRename(symbol, '
    Replace = '|| false && !PublicApi.CanRename(symbol, '
    Tests = 'PascalCaseNamingTests.PublicApi_IsLeftAloneByDefault'
}
