# Seventh trial (2026-10-11): migrate per project, file header escapes, line endings, init re-runs, preview
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'template.IndexOfAny(['';'', ''#'']) >= 0'
    Replace = 'false'
    Tests = 'MigrationTests.Migrate_APlainHeaderWithASemicolon'
}
