# Seventh trial (2026-10-11): migrate per project, file header escapes, line endings, init re-runs, preview
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'files.OrderBy(f => Others(id, f.Key).Length)'
    Replace = 'files.OrderBy(f => 0)'
    Tests = 'MigrationTests.PreviewAttribution_CountsFilesPerReportedRule'
}
