# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = 'return RestoreInputs(root).All(f => File.GetLastWriteTimeUtc(f) < restored);'
    Replace = 'return true;'
    Tests = 'MigrationTests.Format_SkipsTheRestore_OnlyWhenItsOutputIsCurrent'
}
