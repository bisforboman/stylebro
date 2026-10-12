# Trial 12 (2026-10-12): Steeltoe, sourcegit
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'var folder = BlockFolder(root);'
    Replace = 'var folder = root;'
    Tests = 'MigrationTests.UnformattedFolders_AreReadFromTheBlockAbove'
}
