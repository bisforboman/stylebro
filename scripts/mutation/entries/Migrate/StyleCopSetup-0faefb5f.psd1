# Seventh trial (2026-10-11): migrate per project, file header escapes, line endings, init re-runs, preview
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'Directory.EnumerateFiles(folder, Path.GetFileName(path)).FirstOrDefault() ?? path'
    Replace = 'path'
    Tests = 'MigrationTests.PackageHints_NameCentralPackageManagementOnlyWhenTheRepositoryUsesIt'
}
