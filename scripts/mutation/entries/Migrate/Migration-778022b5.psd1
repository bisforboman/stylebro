# Trial 24 (Refit): StyleSharp member order
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'lines.Add(StyleSharpOnly(setup)'
    Replace = 'lines.Add(false'
    Tests = 'MigrationTests.StyleSharp_WritesItsOwnMeaning_WhereItsRuleDiffersFromStyleCops'
}
