# packages.config (trial 11, Mapster.EFCore): only old-style projects restore through it
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '"packages.config")) && IsOldStyle(project);'
    Replace = '"packages.config"));'
    Tests = 'MigrationTests.SdkStyleProjects_WithAStrayPackagesConfig'
}
