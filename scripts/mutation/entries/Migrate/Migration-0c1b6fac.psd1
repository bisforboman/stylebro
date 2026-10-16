# With StyleCop next to StyleSharp, StyleCop meaning applies
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'private static bool StyleSharpOnly(StyleCopSetup setup) => setup.StyleSharp is { WithStyleCop: false };'
    Replace = 'private static bool StyleSharpOnly(StyleCopSetup setup) => setup.StyleSharp is not null;'
    Tests = 'MigrationTests.StyleSharp_WritesItsOwnMeaning_WhereItsRuleDiffersFromStyleCops'
}
