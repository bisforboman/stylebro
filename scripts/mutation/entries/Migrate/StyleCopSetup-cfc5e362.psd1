# Seventh trial (2026-10-11): migrate per project, file header escapes, line endings, init re-runs, preview
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = '.Distinct().Skip(1).Any()))'
    Replace = '.Distinct().Skip(9).Any()))'
    Tests = 'MigrationTests.EachProjectsOwnRulesetAndStyleCopJson'
}
