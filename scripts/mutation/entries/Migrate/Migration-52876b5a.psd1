# StyleSharp read like StyleCop (2026-10-14)
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (setup.StyleSharp is not null && setup.IsOn("SX1309"))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.StyleSharp_IsReadLikeStyleCop_'
}
