# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = 'is not { Success: true } m
            || string.Equals'
    Replace = 'is not { Success: true } m
            || true || string.Equals'
    Tests = 'MigrationTests.Verify_SplitsABuildOfSeveralProjects_IntoEachProjectsOwnOutput'
}
