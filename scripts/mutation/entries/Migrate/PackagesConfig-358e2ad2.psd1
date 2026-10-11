# packages.config projects (2026-10-11): the Error line goes into NuGet's existing target
@{
    File = 'src/StyleBro.Migrate/PackagesConfig.cs'
    Find = 'if (ensure.Success)'
    Replace = 'if (false)'
    Tests = 'MigrationTests.PackagesConfig_GoesNextTo'
}
