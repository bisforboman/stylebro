# Trial 24 (Refit): SST1305 allows id, ok and 11 more words
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'new System.Text.Json.Nodes.JsonArray(HungarianWords.Concat(prefixes)'
    Replace = 'new System.Text.Json.Nodes.JsonArray(prefixes'
    Tests = 'MigrationTests.StyleSharp_'
}
