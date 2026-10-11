# Seventh trial (2026-10-11): migrate per project, file header escapes, line endings, init re-runs, preview
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'setup.Folders is null || setup.FoldersWithout.Count == 0 ? main.Lines'
    Replace = 'true ? main.Lines'
    Tests = 'MigrationTests.Settings_OnlyWhereStyleCopRuns_LikeLiteBus'
}
