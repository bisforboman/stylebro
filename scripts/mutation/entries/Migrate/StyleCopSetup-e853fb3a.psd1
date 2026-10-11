# Seventh trial (2026-10-11): migrate per project, file header escapes, line endings, init re-runs, preview
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'JsonText(config.Json) != JsonText(json)'
    Replace = 'false'
    Tests = 'MigrationTests.EachProjectsOwnRulesetAndStyleCopJson'
}
