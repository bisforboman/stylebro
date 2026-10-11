# Public API is renamed only with stylebro_rename_public_api = true
@{
    File = 'src/StyleBro.Analyzers/Naming/NamespaceNamingAnalyzer.cs'
    Find = '|| !PublicApi.CanRename(ns, options)'
    Replace = ''
    Tests = 'NamespaceNamingTests.NamespacesOfPublicTypes_AreLeftAloneByDefault'
}
