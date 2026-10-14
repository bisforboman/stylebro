# StyleSharp read like StyleCop (2026-10-14)
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'result[id] = styleSharp.WithStyleCop && raw.TryGetValue'
    Replace = 'result[id] = false && raw.TryGetValue'
    Tests = 'MigrationTests.StyleSharp_WithStyleCop_'
}
