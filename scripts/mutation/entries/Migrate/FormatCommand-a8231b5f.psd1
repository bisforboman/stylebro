# A byte order mark is set aside too.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;'
    Replace = 'bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 0 : 0;'
    Tests = 'MigrationTests.Snapshot_EndingsAside_IgnoresLineEndingsAndAByteOrderMark'
}
