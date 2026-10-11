# packages.config projects (2026-10-11): the Analyzer items go next to the project's other ones
@{
    File = 'src/StyleBro.Migrate/PackagesConfig.cs'
    Find = 'if (last is not null)'
    Replace = 'if (false)'
    Tests = 'MigrationTests.PackagesConfig_GoesNextTo'
}
