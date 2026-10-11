# SX1309S counts as covered only with '_camelCase' (2026-10-09)
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'lines.Add($"{StyleBro.Analyzers.Naming.FieldNames.MutableStaticStyleKey} = {result.FieldStyle}");'
    Replace = ''
    Tests = 'MigrationTests.MutableStaticStyle_IsPinned'
}
