# Owner 2026-10-22 (Note it, don't fail): the preview stays clean with only a held-back SDK rule
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'var notVerified = !changed[^1] && putBack.Count == 0 && FormatCommand.Unverified(gaps).Count > 0;'
    Replace = 'var notVerified = !changed[^1] && putBack.Count == 0 && gaps.Count > 0;'
    Tests = 'MigrationTests.Preview_WithOnlyAHeldBackSdkRule'
}
