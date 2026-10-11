# Public API is renamed only with stylebro_rename_public_api = true
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'if (!renamePublicApi && symbols.Any(PublicApi.IsVisible))'
    Replace = 'if (false)'
    Tests = 'PascalCaseNamingTests.APublicImplementation_KeepsTheName'
}
