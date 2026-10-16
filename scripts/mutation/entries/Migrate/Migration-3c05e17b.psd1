# Trial 24 (Refit): SST1300 does not check namespaces (BRO1312 off)
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (StyleSharpOnly(setup) && StyleSharpLeavesAlone.Contains(id))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.StyleSharp_WritesItsOwnMeaning_WhereItsRuleDiffersFromStyleCops'
}
