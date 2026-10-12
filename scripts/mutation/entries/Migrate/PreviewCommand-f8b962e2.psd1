# Trial 8, tool (2026-10-11): --diff stops up front for packages.config projects
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'is { Count: > 0 } packagesConfig)'
    Replace = 'is { Count: < 0 } packagesConfig)'
    Tests = 'MigrationTests.OldStyleProjects_'
}
