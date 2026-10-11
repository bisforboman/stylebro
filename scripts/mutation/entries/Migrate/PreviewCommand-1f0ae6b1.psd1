# Seventh trial (2026-10-11): migrate per project, file header escapes, line endings, init re-runs, preview
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'string.Compare(m.Groups[1].Value, name, StringComparison.OrdinalIgnoreCase) > 0'
    Replace = 'false'
    Tests = 'MigrationTests.Preview_PackageEdits_KeepTheByteOrderMark'
}
