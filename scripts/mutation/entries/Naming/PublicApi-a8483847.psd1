# Public API is renamed only with stylebro_rename_public_api = true
@{
    File = 'src/StyleBro.Analyzers/Naming/PublicApi.cs'
    Find = 'value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)'
    Replace = 'true'
    Tests = 'FieldNamingTests.PublicApi_IsLeftAloneByDefault'
}
