# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '!setup.IsOn("SA1633") ? new HashSet<string>'
    Replace = 'false ? new HashSet<string>'
    Tests = 'MigrationTests.TheReport_LeavesOutHeaderRules_WhenSa1633IsOff'
}
