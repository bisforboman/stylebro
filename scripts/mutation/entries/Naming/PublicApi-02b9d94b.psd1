# Public API is renamed only with stylebro_rename_public_api = true
@{
    File = 'src/StyleBro.Analyzers/Naming/PublicApi.cs'
    Find = '|| ns.GetNamespaceMembers().Any(IsVisible)'
    Replace = ''
    Tests = 'NamespaceNamingTests.NamespacesOfPublicTypes_AreLeftAloneByDefault'
}
