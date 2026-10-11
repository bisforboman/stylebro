# Seventh trial (2026-10-11): migrate per project, file header escapes, line endings, init re-runs, preview
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'var bom = bytes is [0xEF, 0xBB, 0xBF, ..];'
    Replace = 'var bom = false;'
    Tests = 'MigrationTests.Preview_PackageEdits_KeepTheByteOrderMark'
}
