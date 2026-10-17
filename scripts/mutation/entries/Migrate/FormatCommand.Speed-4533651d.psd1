# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = 'is ({ } stamp, _) && stamp > restored)'
    Replace = 'is ({ } stamp, _) && false)'
    Tests = 'MigrationTests.Format_SkipsTheRestore_AfterItsOwnNoOpRestore'
}
