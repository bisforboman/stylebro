# Trial 38: a public name in a public-API listing stays
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'if (names.Files.ApiListings.TryGetValue'
    Replace = 'if (false && names.Files.ApiListings.TryGetValue'
    Tests = 'KeptFindingsTests.APublicApiListing_KeepsAPublicName'
}
