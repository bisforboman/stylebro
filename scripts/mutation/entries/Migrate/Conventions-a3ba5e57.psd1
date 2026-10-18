# bro migrate after the second trial (2026-10-09): no placeholder company, bulk severities vs ruleset entries,
# folder rulesets, init's field style
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (text is null || Migration.IsGeneratedOrVendored(Path.GetRelativePath(root, file), text))'
    Replace = 'if (text is null)'
    Tests = 'MigrationTests'
}
