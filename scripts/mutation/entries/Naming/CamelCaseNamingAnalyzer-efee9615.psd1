# Public API is renamed only with stylebro_rename_public_api = true
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'
    Find = '(renamePublicApi || !PublicApi.IsVisible(baseParameter))'
    Replace = 'true'
    Tests = 'ParameterMatchesBaseTests.PublicApi_IsLeftAloneByDefault'
}
