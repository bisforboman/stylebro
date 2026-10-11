# packages.config projects (2026-10-11): the package step installs into packages.config projects
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'PackagesConfig.Install(project, version);'
    Replace = ''
    Tests = 'MigrationTests.AddPackage_InstallsItThePackagesConfigWay'
}
