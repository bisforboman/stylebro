# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = 'if (projects.Count == 0)'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Format_SkipsTheRestore_OnlyWhenItsOutputIsCurrent'
}
