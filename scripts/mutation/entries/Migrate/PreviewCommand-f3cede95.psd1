# Seventh trial (2026-10-11): migrate per project, file header escapes, line endings, init re-runs, preview
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'KeepLineEndings(text, edit(text)), new UTF8Encoding(bom));'
    Replace = 'KeepLineEndings(text, edit(text)), new UTF8Encoding(false));'
    Tests = 'MigrationTests.Preview_PackageEdits_KeepTheByteOrderMark'
}
