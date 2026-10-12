# Seventh trial (2026-10-11): migrate per project, file header escapes, line endings, init re-runs, preview
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'tab + 1), System.Text.RegularExpressions.Regex.IsMatch(info, @"\battr/text\b")'
    Replace = 'tab + 1), false'
    Tests = 'MigrationTests.Conventions_Converted_AreFilesGitConvertsOnCheckout'
}
