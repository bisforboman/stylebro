# Seventh trial (2026-10-11): migrate per project, file header escapes, line endings, init re-runs, preview
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'allConfigs.Where(c => projects.Uses[c.Project] ||'
    Replace = 'allConfigs.Where(c => true ||'
    Tests = 'MigrationTests.EachProjectsOwnRulesetAndStyleCopJson'
}
