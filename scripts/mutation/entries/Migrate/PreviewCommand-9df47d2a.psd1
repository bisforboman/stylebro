# packages.config projects (2026-10-11): the props' PackageReference stays away from packages.config projects
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '? PackagesConfig.NotPackagesConfig'
    Replace = '? null'
    Tests = 'MigrationTests.AddPackage_InstallsItThePackagesConfigWay'
}
