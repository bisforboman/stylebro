# Trial 12 (2026-10-12): Steeltoe, sourcegit
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '.Where(f => f != "." && !f.StartsWith("../", StringComparison.Ordinal) && f != "..")'
    Replace = ''
    Tests = 'MigrationTests.UnformattedFolders_AreReadFromTheBlockAbove'
}
