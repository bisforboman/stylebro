# Seventh trial (2026-10-11): migrate per project, file header escapes, line endings, init re-runs, preview
@{
    File = 'src/StyleBro.Analyzers/Documentation/FileHeaders.cs'
    Find = '|| i < first || i >= last ?'
    Replace = '?'
    Tests = 'FileHeaderTests'
}
