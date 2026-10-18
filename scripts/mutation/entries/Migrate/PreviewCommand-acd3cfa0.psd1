# Trial 31: the preview excludes a packages folder inside the repository
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'var packages = PackagesFolderInside(folder) is { } inside ? $"/{inside}/\n" : string.Empty;'
    Replace = 'var packages = string.Empty;'
    Tests = 'MigrationTests.Preview_LeavesAPackagesFolder'
}
