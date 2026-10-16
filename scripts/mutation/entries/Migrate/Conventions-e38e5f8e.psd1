# Trial 25 (2026-10-16): migrate/init
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (changing * 2 <= total)'
    Replace = 'if (true)'
    Tests = 'MigrationTests.Init_NamespaceBraces'
}
