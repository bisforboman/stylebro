# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (group.Count > 1 && (together ?? BuildTogether)(group, configuration) is { ExitCode: 0 } all)'
    Replace = 'if (group.Count > 0 && (together ?? BuildTogether)(group, configuration) is { ExitCode: 0 } all)'
    Tests = 'MigrationTests.Verify_BuildsTheGeneratorConfigurationsTogether_AndAloneWhenThatFails'
}
