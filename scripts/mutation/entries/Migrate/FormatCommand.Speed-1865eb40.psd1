# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = 'expected.EnumerateArray().All(e => e.GetString() is { } path && File.Exists(path))'
    Replace = 'true'
    Tests = 'MigrationTests.Format_SkipsTheRestore_OnlyWhenItsOutputIsCurrent'
}
