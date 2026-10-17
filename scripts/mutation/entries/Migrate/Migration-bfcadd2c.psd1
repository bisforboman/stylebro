# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'var rootUnread = ReadOrNull(own) is null;'
    Replace = 'var rootUnread = own is null;'
    Tests = 'MigrationTests.Init_SaysWhichFilesItCouldntRead'
}
