# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = '|| !RestoreSucceeded(cache) '
    Replace = ''
    Tests = 'MigrationTests.Format_SkipsTheRestore_OnlyWhenItsOutputIsCurrent'
}
