# Public API is renamed only with stylebro_rename_public_api = true
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNamingAnalyzer.cs'
    Find = '&& PublicApi.CanRename(field, options)'
    Replace = ''
    Tests = 'FieldNamingTests.PublicApi_IsLeftAloneByDefault'
}
