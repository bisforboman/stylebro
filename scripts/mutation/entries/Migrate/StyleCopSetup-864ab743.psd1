# StyleSharp read like StyleCop (2026-10-14)
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'q.Equals("require_this", StringComparison.OrdinalIgnoreCase) ? "SA1101" : "SX1101"'
    Replace = 'false ? "SA1101" : "SX1101"'
    Tests = 'MigrationTests.StyleSharp_Options_'
}
