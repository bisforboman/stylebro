# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '.Where(f => f != own && (rootUnread || ReadOrNull(f) is null))'
    Replace = '.Where(f => f != own && rootUnread)'
    Tests = 'MigrationTests.Init_SaysWhichFilesItCouldntRead'
}
