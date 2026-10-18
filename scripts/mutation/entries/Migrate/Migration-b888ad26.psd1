# bro migrate after the second trial (2026-10-09): no placeholder company, bulk severities vs ruleset entries,
# folder rulesets, init's field style
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = ' || CompanyName(setup) is null))'
    Replace = '))'
    Tests = 'MigrationTests'
}
