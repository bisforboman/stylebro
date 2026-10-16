# Trial 25 (2026-10-16): migrate/init
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'v.Trim() is "all" or "types"'
    Replace = 'v.Trim() is "all"'
    Tests = 'MigrationTests.Init_NamespaceBraces'
}
