# Only the rules format checks count in the build of a generator project.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Where(m => ids.Contains(m.Groups["id"].Value) && '
    Replace = '.Where(m => '
    Tests = 'MigrationTests.Verify_BuildsTheConfigurationsFormatSkipsForAGeneratorProject'
}
