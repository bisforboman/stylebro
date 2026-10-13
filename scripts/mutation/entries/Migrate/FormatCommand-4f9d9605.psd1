# Trial 14: a not-built project's folder holding another project isn't left out
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '&& !projects.Any(p => !string.Equals(p, full'
    Replace = '&& !projects.Take(0).Any(p => !string.Equals(p, full'
    Tests = 'MigrationTests.NotBuiltFolders_AreTheProjectsNoConfigurationBuilds'
}
