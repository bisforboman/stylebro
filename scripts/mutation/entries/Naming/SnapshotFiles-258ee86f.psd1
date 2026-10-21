# Trial 38: a public-API listing doesn't hold private names
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = 'Add(IsApiListing(text) ? words.ApiListings : words.Snapshots, path, text)'
    Replace = 'Add(words.Snapshots, path, text)'
    Tests = 'KeptFindingsTests.APublicApiListing_KeepsNoPrivateName'
}
