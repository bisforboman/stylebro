# Public API is renamed only with stylebro_rename_public_api = true
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'
    Find = '|| !PublicApi.CanRename(parameter, '
    Replace = '|| false && !PublicApi.CanRename(parameter, '
    Tests = 'CamelCaseNamingTests.PublicApiParameters_AreLeftAloneByDefault'
}
