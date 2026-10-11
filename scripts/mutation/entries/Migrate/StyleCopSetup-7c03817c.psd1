# stylebro-migrate after the second trial (2026-10-09): no placeholder company, bulk severities vs ruleset entries,
# folder rulesets, init's field style
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = '!config.Bulk.Contains(p.Key) || !specific.Contains(p.Key)'
    Replace = 'true'
    Tests = 'MigrationTests'
}
