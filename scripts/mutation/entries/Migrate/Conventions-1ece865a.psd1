# stylebro-migrate after the second trial (2026-10-09): no placeholder company, bulk severities vs ruleset entries,
# folder rulesets, init's field style
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = ': second >= Share * total ? 1 : first >= Share * total ? 0 : -1;'
    Replace = ': second > first ? 1 : 0;'
    Tests = 'MigrationTests'
}
