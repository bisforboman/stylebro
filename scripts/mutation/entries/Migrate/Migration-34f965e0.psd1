# stylebro-migrate: pinned static field casing, nested repositories, bulk severity, IDE0073 vs SA1636
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (setup.IsOn("SA1633") && textOff.Count == 0)'
    Replace = 'if (setup.IsOn("SA1633"))'
    Tests = 'MigrationTests'
}
