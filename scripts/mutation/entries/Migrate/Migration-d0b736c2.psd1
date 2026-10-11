# stylebro-migrate after the second trial (2026-10-09): no placeholder company, bulk severities vs ruleset entries,
# folder rulesets, init's field style
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = ' || !order.Contains("accessibility")'
    Replace = ''
    Tests = 'MigrationTests'
}
