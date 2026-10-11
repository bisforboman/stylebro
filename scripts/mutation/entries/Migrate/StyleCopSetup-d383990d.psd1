# Seventh trial (2026-10-11): migrate per project, file header escapes, line endings, init re-runs, preview
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'if (differences.Count == 0 && own is null)'
    Replace = 'if (true)'
    Tests = 'MigrationTests.EachProjectsOwnRulesetAndStyleCopJson'
}
