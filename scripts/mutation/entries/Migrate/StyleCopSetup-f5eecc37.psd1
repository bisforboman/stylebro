# bro migrate after the second trial (2026-10-09): no placeholder company, bulk severities vs ruleset entries,
# folder rulesets, init's field style
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'rulesets.Add([]);'
    Replace = ''
    Tests = 'MigrationTests'
}
