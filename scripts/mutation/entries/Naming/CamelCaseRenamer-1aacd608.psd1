# Trial 38: only public names are held by a public-API listing
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'names.Files.ApiListings.TryGetValue(symbol.Name, out var listing) && IsInPublicApi(symbol)'
    Replace = 'names.Files.ApiListings.TryGetValue(symbol.Name, out var listing)'
    Tests = 'KeptFindingsTests.APublicApiListing_KeepsNoPrivateName'
}
