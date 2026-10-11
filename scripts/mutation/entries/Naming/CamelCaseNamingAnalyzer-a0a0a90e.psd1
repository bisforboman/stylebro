# Public API is renamed only with stylebro_rename_public_api = true
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'
    Find = 'if ((!renamePublicApi && PublicApi.IsVisible(parameter))'
    Replace = 'if (false'
    Tests = 'ParameterMatchesBaseTests.PublicApi_IsLeftAloneByDefault'
}
