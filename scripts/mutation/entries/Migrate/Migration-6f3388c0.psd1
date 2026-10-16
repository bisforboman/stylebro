# Trial 24 (Refit): SST1413 does not check switch expressions or with
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '== true || StyleSharpOnly(setup))'
    Replace = '== true)'
    Tests = 'MigrationTests.StyleSharp_WritesItsOwnMeaning_WhereItsRuleDiffersFromStyleCops'
}
