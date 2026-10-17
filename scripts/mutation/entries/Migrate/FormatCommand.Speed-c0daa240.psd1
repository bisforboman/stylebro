# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = '!lines.SkipLast(1).SequenceEqual(StampLines(assetsFiles)) || '
    Replace = ''
    Tests = 'MigrationTests.Format_SkipsTheRestore_AfterItsOwnNoOpRestore'
}
