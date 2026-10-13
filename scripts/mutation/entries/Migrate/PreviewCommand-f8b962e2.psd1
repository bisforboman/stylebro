# Trial 8, tool (2026-10-11): --diff stops up front for packages.config projects
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'if (packagesConfig.Count > 0 && packagesConfig.Count =='
    Replace = 'if (packagesConfig.Count < 0 && packagesConfig.Count =='
    Tests = 'MigrationTests.OldStyleProjects_'
}
