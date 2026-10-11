# Public API is renamed only with stylebro_rename_public_api = true
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '|| (!renamePublicApi && PublicApi.IsVisible(current))'
    Replace = ''
    Tests = 'CamelCaseNamingTests.PublicApiParameters_AreLeftAloneByDefault'
}
