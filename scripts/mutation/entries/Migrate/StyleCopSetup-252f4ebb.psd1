# Trial 24 (Refit): SST1629 checks summary, remarks, returns and value only
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = '["excludeFromPunctuationCheck"] = new System.Text.Json.Nodes.JsonArray('
    Replace = '["unused"] = new System.Text.Json.Nodes.JsonArray('
    Tests = 'MigrationTests.StyleSharp_WritesItsOwnMeaning_WhereItsRuleDiffersFromStyleCops'
}
