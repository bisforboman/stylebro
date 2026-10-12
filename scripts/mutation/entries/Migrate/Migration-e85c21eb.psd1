# Trial 12 (2026-10-12): Steeltoe, sourcegit
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (Directory.Exists(Path.Combine(folder.FullName, ".git")) || File.Exists(Path.Combine(folder.FullName, ".git")))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.UnformattedFolders_AreReadFromTheBlockAbove'
}
