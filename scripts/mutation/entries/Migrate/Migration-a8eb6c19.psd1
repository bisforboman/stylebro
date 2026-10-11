# SX1309S counts as covered only with '_camelCase' (2026-10-09)
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (result.FieldStyle != "_camelCase")'
    Replace = 'if (false)'
    Tests = 'MigrationTests.StaticUnderscoreAlone'
}
