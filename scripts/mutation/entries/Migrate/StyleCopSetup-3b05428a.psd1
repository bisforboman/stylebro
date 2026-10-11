# stylebro-migrate: pinned static field casing, nested repositories, bulk severity, IDE0073 vs SA1636
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = '&& !IsNestedRepository(child)'
    Replace = ''
    Tests = 'MigrationTests'
}
