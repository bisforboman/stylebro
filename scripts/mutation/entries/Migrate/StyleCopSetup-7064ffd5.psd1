# StyleSharp read like StyleCop (2026-10-14)
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'fromStyleSharp[id] = fromStyleSharp.TryGetValue(id, out var before) && before < severity ? before : severity;'
    Replace = 'fromStyleSharp[id] = severity;'
    Tests = 'MigrationTests.StyleSharp_Options_'
}
