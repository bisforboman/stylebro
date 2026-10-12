# Trial 10, tool (2026-10-12): --diff refuses only packages.config projects
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '.Where(p => FormatCommand.UsesPackagesConfig(Path.Combine(pathFolder, p)))'
    Replace = ''
    Tests = 'MigrationTests.Preview_OldStyleProjectsWithoutPackagesConfig_'
}
