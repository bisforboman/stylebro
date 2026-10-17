# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = '=> !noRestore && !forceRestore && current();'
    Replace = '=> !noRestore && current();'
    Tests = 'MigrationTests.Format_SkipsTheRestore_OnlyWhenItsOutputIsCurrent'
}
