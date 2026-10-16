# Only the rules format checks count in the build of a generator project.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Where(m => m.Success && ids.Contains(m.Groups["id"].Value) &&'
    Replace = '.Where(m => m.Success &&'
    Tests = 'MigrationTests.Verify_BuildsTheConfigurationsFormatSkipsForAGeneratorProject'
}
