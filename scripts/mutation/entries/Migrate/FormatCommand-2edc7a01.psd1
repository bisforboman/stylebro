# Only the findings in the generator project itself count.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '(!m.Groups["project"].Success || string.Equals(Path.GetFullPath(m.Groups["project"].Value), Path.GetFullPath(project), StringComparison.OrdinalIgnoreCase))'
    Replace = 'true'
    Tests = 'MigrationTests.Verify_BuildsTheConfigurationsFormatSkipsForAGeneratorProject'
}
