# StyleSharp read like StyleCop (2026-10-14)
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = '"^stylesharp_", "stylesharp."'
    Replace = '"^stylesharp_x", "stylesharp."'
    Tests = 'MigrationTests.StyleSharp_Options_'
}
