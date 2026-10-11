# stylebro-migrate: pinned static field casing, nested repositories, bulk severity, IDE0073 vs SA1636
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = '(defaultSeverity > Severity.None || severity == Severity.None)'
    Replace = 'true'
    Tests = 'MigrationTests'
}
